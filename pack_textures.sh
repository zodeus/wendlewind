cd /mnt/c/Users/hawkk/dev-personal/wendlewind/
rm -rf Wendlewind/bin/Textures
mkdir -p Wendlewind/bin/Textures/ 
find Wendlewind/Content/Textures/UI -type f -name "*.png" -exec cp {} Wendlewind/bin/Textures \;
./MyraTexturePacker.0.9.2/MyraTexturePacker.exe Wendlewind/bin/Textures Wendlewind/Content/UI/milgreth_ui_skin.png 4096 4096
