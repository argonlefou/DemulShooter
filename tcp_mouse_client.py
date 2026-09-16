#!/usr/bin/env python3
"""TCP input client for DemulShooter and BepInEx gun game plugins.

Device type (gun vs mouse) is auto-detected: absolute-axis devices are treated
as light guns (GunState), relative-axis devices as mice (MouseState).
Offscreen reload: when a gun aims outside the screen and fires, trigger is
converted to reload automatically.

Button remapping: extra gun buttons can be forwarded as keyboard keys via a
UInput virtual device, independently of the DS bridge state.

Supported games: demul, rha, wws, tra, owr, mib, mia, nha2, marss, pvz, pbx, rgs

Examples:
  %(prog)s --game demul --gun1 /dev/input/event0
  %(prog)s --game wws --gun1 /dev/input/event0 --gun2 /dev/input/event1
  %(prog)s --game tra --width 1280 --height 1024 \\
           --gun1 /dev/input/event2 --gun2 /dev/input/event3
"""

import argparse
import glob
import re
import select
import signal
import socket
import struct
import subprocess
import sys
import time

try:
    from evdev import InputDevice, UInput, ecodes
except ImportError:
    print("Missing dependency: python-evdev. Install with `pip install evdev`.")
    sys.exit(1)

DEFAULT_HOST = "127.0.0.1"
DEFAULT_PORT = 33610
DEFAULT_SCREEN_WIDTH = 1920
DEFAULT_SCREEN_HEIGHT = 1080

RECONNECT_INTERVAL = 2.0

# Per-player button → key mappings (Start and Coin differ per player slot)
PLAYER_MAPPING = [
    {"BTN_MIDDLE": "KEY_1", "BTN_1": "KEY_5"},   # P1: Start=1, Coin=5
    {"BTN_MIDDLE": "KEY_2", "BTN_1": "KEY_6"},   # P2: Start=2, Coin=6
    {"BTN_MIDDLE": "KEY_3", "BTN_1": "KEY_7"},   # P3: Start=3, Coin=7
    {"BTN_MIDDLE": "KEY_4", "BTN_1": "KEY_8"},   # P4: Start=4, Coin=8
]

# Shared mappings applied to all players (None = pass through unmapped)
SHARED_MAPPING = {
    "BTN_2": None,
    "BTN_3": None,
    "BTN_4": None,
    "BTN_5": "KEY_UP",
    "BTN_6": "KEY_DOWN",
    "BTN_7": "KEY_LEFT",
    "BTN_8": "KEY_RIGHT",
}


# ─── Screen size detection ────────────────────────────────────────────────────

def get_screen_size():
    # xrandr: Xorg and XWayland — parses "1920x1080+0+0" on connected output lines
    try:
        out = subprocess.check_output(
            ['xrandr', '--current'], text=True, stderr=subprocess.DEVNULL, timeout=2)
        m = re.search(r'(\d+)x(\d+)\+\d+\+\d+', out)
        if m:
            return int(m.group(1)), int(m.group(2))
    except Exception:
        pass

    # wlr-randr: native Wayland (wlroots compositors) — "1920x1080 px, ... (current)"
    try:
        out = subprocess.check_output(
            ['wlr-randr'], text=True, stderr=subprocess.DEVNULL, timeout=2)
        m = re.search(r'(\d+)x(\d+) px[^\n]*current', out)
        if m:
            return int(m.group(1)), int(m.group(2))
    except Exception:
        pass

    # DRM sysfs: kernel-level current mode, works without any display server
    for path in sorted(glob.glob('/sys/class/drm/card*-*/mode')):
        try:
            with open(path) as f:
                m = re.match(r'(\d+)x(\d+)', f.read().strip())
            if m:
                return int(m.group(1)), int(m.group(2))
        except Exception:
            pass

    return DEFAULT_SCREEN_WIDTH, DEFAULT_SCREEN_HEIGHT


# ─── Gun state (absolute-position devices) ───────────────────────────────────

class GunState:
    def __init__(self, x_min=0, x_max=65535, y_min=0, y_max=65535):
        self.x = 0
        self.y = 0
        self.x_min = x_min
        self.x_max = x_max
        self.y_min = y_min
        self.y_max = y_max
        self.trigger = 0
        self.reload = 0
        self.action = 0

    def is_offscreen(self):
        if self.x_max > self.x_min:
            nx = (self.x - self.x_min) / (self.x_max - self.x_min)
            if nx <= 0.0 or nx >= 1.0:
                return True
        if self.y_max > self.y_min:
            ny = (self.y - self.y_min) / (self.y_max - self.y_min)
            if ny <= 0.0 or ny >= 1.0:
                return True
        return False

    def pixel_x(self, screen_w):
        if self.x_max <= self.x_min:
            return 0.0
        n = (self.x - self.x_min) / (self.x_max - self.x_min)
        return max(0.0, min(1.0, n)) * screen_w

    def pixel_y(self, screen_h):
        if self.y_max <= self.y_min:
            return 0.0
        n = (self.y - self.y_min) / (self.y_max - self.y_min)
        return max(0.0, min(1.0, n)) * screen_h


# ─── Mouse state (relative-position devices) ─────────────────────────────────

class MouseState:
    def __init__(self, screen_w=1920, screen_h=1080):
        self.px = float(screen_w // 2)
        self.py = float(screen_h // 2)
        self.screen_w = screen_w
        self.screen_h = screen_h
        self.trigger = 0
        self.reload = 0
        self.action = 0

    def is_offscreen(self):
        return False

    def move(self, dx, dy):
        self.px = max(0.0, min(float(self.screen_w), self.px + dx))
        self.py = max(0.0, min(float(self.screen_h), self.py + dy))

    def pixel_x(self, *_):
        return self.px

    def pixel_y(self, *_):
        return self.py


# ─── Packet builders ──────────────────────────────────────────────────────────

def build_ds_packet(states, game, scr_w, scr_h):
    ax = [s.pixel_x(scr_w) for s in states]
    ay = [scr_h - s.pixel_y(scr_h) for s in states]
    offscreen = [s.is_offscreen() and bool(s.trigger) for s in states]
    t = [0 if of else s.trigger for s, of in zip(states, offscreen)]
    r = [1 if (s.reload or of) else 0 for s, of in zip(states, offscreen)]
    a = [s.action for s in states]

    while len(ax) < 4:
        ax.append(0.0); ay.append(0.0); t.append(0); r.append(0); a.append(0)

    if game == 'rha':
        # 4P: Axis_X[4] Axis_Y[4] EnableInputsHack HideCrosshairs Trigger[4] = 38
        return struct.pack('<ffffffff BB BBBB', *ax[:4], *ay[:4], 1, 0, *t[:4])
    elif game == 'wws':
        # 2P: Axis_X[2] Axis_Y[2] EnableInputsHack HideCrosshairs Reload[2] Trigger[2] = 22
        return struct.pack('<ffff BB BB BB', *ax[:2], *ay[:2], 1, 0, *r[:2], *t[:2])
    elif game == 'tra':
        # 4P: Axis_X[4] Axis_Y[4] EnableInputsHack HideCrosshairs Reload[4] Trigger[4] = 42
        return struct.pack('<ffffffff BB BBBB BBBB', *ax[:4], *ay[:4], 1, 0, *r[:4], *t[:4])
    elif game == 'owr':
        # 2P: Axis_X[2] Axis_Y[2] ChangeWeapon[2] EnableInputsHack HideCrosshairs Reload[2] Trigger[2] = 24
        return struct.pack('<ffff BB BB BB BB', *ax[:2], *ay[:2], *a[:2], 1, 0, *r[:2], *t[:2])
    elif game == 'mib':
        # 2P: Axis_X[2] Axis_Y[2] EnableInputsHack HideCrosshairs HideGuns Trigger[2] = 21
        return struct.pack('<ffff BBB BB', *ax[:2], *ay[:2], 1, 0, 0, *t[:2])
    elif game == 'mia':
        # 2P: Axis_X[2] Axis_Y[2] EnableInputsHack HideCrosshairs Reload[2] TriggerL[2] TriggerR[2] = 24
        return struct.pack('<ffff BB BB BB BB', *ax[:2], *ay[:2], 1, 0, *r[:2], *t[:2], 0, 0)
    elif game == 'nha2':
        # 2P: Action[2] Axis_X[2] Axis_Y[2] ChangeWeapon[2] EnableInputsHack HideCrosshairs HideGuns Trigger[2] = 25
        return struct.pack('<BB ffff BB BBB BB', *a[:2], *ax[:2], *ay[:2], *r[:2], 1, 0, 0, *t[:2])
    elif game == 'marss':
        # 4P: Axis_X[4] Axis_Y[4] ChangeWeapon[4] EnableInputsHack HideCrosshairs Trigger[4] = 42
        return struct.pack('<ffffffff BBBB BB BBBB', *ax[:4], *ay[:4], *r[:4], 1, 0, *t[:4])
    elif game == 'pvz':
        # 1P: Axis_X[1] Axis_Y[1] EnableInputsHack HideCrosshairs Trigger[1] = 11
        return struct.pack('<ff BB B', ax[0], ay[0], 1, 0, t[0])
    elif game == 'demul':
        # 4P: Axis_X[4] Axis_Y[4] EnableInputsHack HideCrosshairs Trigger[4] Reload[4] Action[4] = 46 bytes
        # Normalize to 0.0-1.0 (DemulShooter TCP server handles its own coordinate system)
        dx = [x / scr_w for x in ax]
        dy = [(scr_h - y) / scr_h for y in ay]
        return struct.pack('<ffffffff BB BBBB BBBB BBBB',
                           *dx[:4], *dy[:4], 1, 0, *t[:4], *r[:4], *a[:4])
    else:
        # pbx / rgs (default): PBX, DRK, RTNA — 2P = 20 bytes
        return struct.pack('<ffff BB BB', *ax[:2], *ay[:2], 1, 0, *t[:2])


# ─── Device helpers ───────────────────────────────────────────────────────────

def build_mapping(player_num):
    """Merge SHARED_MAPPING and PLAYER_MAPPING for a player and resolve to ecodes."""
    raw = dict(SHARED_MAPPING)
    if player_num - 1 < len(PLAYER_MAPPING):
        raw.update(PLAYER_MAPPING[player_num - 1])
    result = {}
    for src_name, dst_name in raw.items():
        if dst_name is None:
            continue
        src_code = ecodes.ecodes.get(src_name)
        dst_code = ecodes.ecodes.get(dst_name)
        if src_code is not None and dst_code is not None:
            result[src_code] = dst_code
    return result


def open_player(path, player_num, screen_w, screen_h, mapping=None):
    dev = InputDevice(path)
    caps = dev.capabilities(verbose=False)
    abs_codes = [c[0] if isinstance(c, tuple) else c for c in caps.get(ecodes.EV_ABS, [])]

    if ecodes.ABS_X in abs_codes and ecodes.ABS_Y in abs_codes:
        state = GunState()
        try:
            info = dev.absinfo(ecodes.ABS_X)
            state.x_min, state.x_max = info.min, info.max
        except Exception:
            pass
        try:
            info = dev.absinfo(ecodes.ABS_Y)
            state.y_min, state.y_max = info.min, info.max
        except Exception:
            pass
        is_mouse = False
        kind = "gun"
    else:
        state = MouseState(screen_w=screen_w, screen_h=screen_h)
        is_mouse = True
        kind = "mouse"

    mapping = mapping or {}
    uinput = None
    if mapping:
        uinput = UInput({ecodes.EV_KEY: list(set(mapping.values()))},
                        name=f"Gun Keys P{player_num}")

    try:
        dev.grab()
    except Exception as e:
        print(f"Warning: could not grab {path}: {e}", file=sys.stderr)

    suffix = f" [{len(mapping)} remaps]" if mapping else ""
    print(f"P{player_num}: {kind} {path} ({dev.name}){suffix}")
    return {"dev": dev, "state": state, "is_mouse": is_mouse, "player": player_num,
            "mapping": mapping, "uinput": uinput}


def apply_mapping(player, event):
    """Forward a mapped button event to the player's UInput virtual keyboard."""
    ui = player.get("uinput")
    if not ui or event.type != ecodes.EV_KEY:
        return
    dst = player["mapping"].get(event.code)
    if dst is None:
        return
    try:
        ui.write(ecodes.EV_KEY, dst, event.value)
        ui.syn()
    except Exception as e:
        print(f"P{player['player']} UInput write error: {e}", file=sys.stderr)


def process_event(player, event):
    """Update player state from one evdev event. Returns True on EV_SYN."""
    if event.type == ecodes.EV_SYN:
        return True

    st = player["state"]

    if player["is_mouse"]:
        if event.type == ecodes.EV_REL:
            if event.code == ecodes.REL_X:
                st.move(event.value, 0)
            elif event.code == ecodes.REL_Y:
                st.move(0, event.value)
        elif event.type == ecodes.EV_KEY:
            if event.code == ecodes.BTN_LEFT:
                st.trigger = 1 if event.value else 0
            elif event.code == ecodes.BTN_RIGHT:
                st.reload = 1 if event.value else 0
            elif event.code == ecodes.BTN_MIDDLE:
                st.action = 1 if event.value else 0
    else:
        if event.type == ecodes.EV_ABS:
            if event.code == ecodes.ABS_X:
                st.x = event.value
            elif event.code == ecodes.ABS_Y:
                st.y = event.value
        elif event.type == ecodes.EV_KEY:
            if event.code == ecodes.BTN_LEFT:
                st.trigger = 1 if event.value else 0
            elif event.code == ecodes.BTN_RIGHT:
                st.reload = 1 if event.value else 0
            elif event.code == ecodes.BTN_MIDDLE:
                st.action = 1 if event.value else 0

    return False


# ─── Main ─────────────────────────────────────────────────────────────────────

def main():
    parser = argparse.ArgumentParser(
        description="TCP input client for DemulShooter and BepInEx gun game plugins.",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=__doc__,
    )
    parser.add_argument("--host", default=DEFAULT_HOST, help="TCP server host (default: 127.0.0.1)")
    parser.add_argument("--port", type=int, default=DEFAULT_PORT, help="TCP server port (default: 9999)")
    parser.add_argument("--width", type=int, default=None, help="Screen width (auto-detected if omitted)")
    parser.add_argument("--height", type=int, default=None, help="Screen height (auto-detected if omitted)")
    parser.add_argument("--game", default="demul", metavar="GAME",
                        choices=["demul", "rha", "wws", "tra", "owr", "mib", "mia",
                                 "nha2", "marss", "pvz", "pbx", "rgs"],
                        help="Game/engine packet format (default: demul)")
    parser.add_argument("--gun1", metavar="PATH", help="evdev device for player 1")
    parser.add_argument("--gun2", metavar="PATH", help="evdev device for player 2")
    parser.add_argument("--gun3", metavar="PATH", help="evdev device for player 3")
    parser.add_argument("--gun4", metavar="PATH", help="evdev device for player 4")
    args = parser.parse_args()

    gun_paths = [(i + 1, p) for i, p in
                 enumerate([args.gun1, args.gun2, args.gun3, args.gun4]) if p]

    if not gun_paths:
        parser.error("Specify at least one device via --gun1 / --gun2 / --gun3 / --gun4")

    if args.width and args.height:
        screen_w, screen_h = args.width, args.height
    else:
        screen_w, screen_h = get_screen_size()
        if args.width:
            screen_w = args.width
        if args.height:
            screen_h = args.height
        print(f"Auto-detected screen: {screen_w}x{screen_h}")

    players = []
    for player_num, path in gun_paths:
        mapping = build_mapping(player_num)
        try:
            players.append(open_player(path, player_num, screen_w, screen_h, mapping=mapping))
        except Exception as e:
            print(f"Cannot open {path}: {e}", file=sys.stderr)
            sys.exit(1)

    print(f"Game: {args.game} | Screen: {screen_w}x{screen_h} | Server: {args.host}:{args.port}")

    sock = None
    last_connect_attempt = 0.0
    running = True

    def stop(sig, frame):
        nonlocal running
        running = False

    signal.signal(signal.SIGTERM, stop)
    signal.signal(signal.SIGINT, stop)

    try:
        while running:
            if sock is None:
                now = time.monotonic()
                if now - last_connect_attempt >= RECONNECT_INTERVAL:
                    last_connect_attempt = now
                    try:
                        sock = socket.create_connection((args.host, args.port), timeout=1)
                        sock.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)
                        print(f"Connected to {args.host}:{args.port}")
                    except Exception as e:
                        print(f"Connection failed: {e}")
                        sock = None

            fds = [p["dev"].fd for p in players]
            if sock is not None:
                fds.append(sock.fileno())

            try:
                readable, _, _ = select.select(fds, [], [], 0.05)
            except Exception:
                continue

            if sock is not None and sock.fileno() in readable:
                try:
                    if not sock.recv(4096):
                        print("Server disconnected.")
                        sock.close()
                        sock = None
                except Exception:
                    sock = None

            changed = False
            for player in list(players):
                if player["dev"].fd not in readable:
                    continue
                try:
                    for event in player["dev"].read():
                        apply_mapping(player, event)
                        if process_event(player, event):
                            changed = True
                except OSError as e:
                    print(f"P{player['player']} device error: {e}")
                    try:
                        player["dev"].close()
                    except Exception:
                        pass
                    players.remove(player)
                    if not players:
                        running = False

            if changed and sock is not None:
                states = [p["state"] for p in players]
                packet = build_ds_packet(states, args.game, screen_w, screen_h)
                try:
                    sock.sendall(packet)
                except Exception as e:
                    print(f"Send error: {e}")
                    sock = None

    finally:
        if sock:
            try:
                sock.close()
            except Exception:
                pass
        for player in players:
            if player.get("uinput"):
                try:
                    player["uinput"].close()
                except Exception:
                    pass
            try:
                player["dev"].ungrab()
            except Exception:
                pass
            try:
                player["dev"].close()
            except Exception:
                pass
        print("Stopped.")


if __name__ == "__main__":
    main()
