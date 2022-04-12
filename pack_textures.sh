cd /mnt/c/Users/hawkk/games/Grafted/
rm -rf Grafted/bin/Textures
mkdir -p Grafted/bin/Textures/ 
find Grafted/Content/Textures/UI -type f -name "*.png" -exec cp {} Grafted/bin/Textures \;
./MyraTexturePacker.0.9.2/MyraTexturePacker.exe Grafted/bin/Textures Grafted/Content/UI/milgreth_ui_skin.png 4096 4096