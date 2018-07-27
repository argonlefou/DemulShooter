#!/bin/bash
echo ""
echo "+------------+"
echo "| git status |"
echo "+------------+"
git status
echo ""
read -p "Press enter to continue..."
echo ""

echo ""
echo "+------------+"
echo "| git add -A |"
echo "+------------+"
git add -A
echo ""
echo "Enter the commit message :"
read -r message

echo ""
echo "+----------------------------+"
echo "| git commit -m \"$message\""
echo "+----------------------------+"
git commit -m "$message"

echo ""
echo "+----------------------------+"
echo "| git push -u origin master  |"
echo "+----------------------------+"
git push -u origin master