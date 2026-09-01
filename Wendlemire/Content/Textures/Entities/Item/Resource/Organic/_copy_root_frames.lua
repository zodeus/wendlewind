-- Aseprite: open a root food PNG, then File > Scripts > this file.
-- Copies frame 1 onto 4 frames and adds an empty "steam" layer to paint on.
-- Export frames 2-4 as Name_1.png / Name_2.png / Name_3.png.
if not app.activeSprite then
  app.alert("Open a root food PNG first.")
  return
end

local spr = app.activeSprite
app.transaction(function()
  local src = spr.layers[1]:cel(spr.frames[1])
  while #spr.frames < 4 do
    local frame = spr:newFrame()
    spr.layers[1]:newCel(frame, src.image:clone(), src.position)
  end
  local steam = spr:newLayer()
  steam.name = "steam"
end)
app.refresh()
