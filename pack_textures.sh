cd /mnt/c/Users/hawkk/dev-personal/wendlewind/
rm -rf Wendlemire/bin/Textures
mkdir -p Wendlemire/bin/Textures/ 
find Wendlemire/Content/Textures/UI -type f -name "*.png" -exec cp {} Wendlemire/bin/Textures \;
./MyraTexturePacker.0.9.2/MyraTexturePacker.exe Wendlemire/bin/Textures Wendlemire/Content/UI/milgreth_ui_skin.png 4096 4096
