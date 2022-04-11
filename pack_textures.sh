cd /mnt/c/Users/hawkk/games/Milgreth/
rm -rf Milgreth/bin/Textures
mkdir -p Milgreth/bin/Textures/ 
find Milgreth/Content/Textures/UI -type f -name "*.png" -exec cp {} Milgreth/bin/Textures \;
./MyraTexturePacker.0.9.2/MyraTexturePacker.exe Milgreth/bin/Textures Milgreth/Content/UI/milgreth_ui_skin.png 4096 4096