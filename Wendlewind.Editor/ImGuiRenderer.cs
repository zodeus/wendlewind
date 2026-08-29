using System.Runtime.InteropServices;
using Num = System.Numerics;

namespace Wendlewind.Editor;

/// <summary>
/// Minimal MonoGame backend for ImGui.NET (font atlas + input + draw lists).
/// </summary>
public sealed class ImGuiRenderer : IDisposable
{
    private readonly Game _game;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly BasicEffect _effect;
    private readonly RasterizerState _rasterizerState = new()
    {
        CullMode = CullMode.None,
        DepthBias = 0,
        FillMode = FillMode.Solid,
        MultiSampleAntiAlias = false,
        ScissorTestEnable = true,
        SlopeScaleDepthBias = 0
    };

    private byte[] _vertexData = [];
    private int _vertexBufferSize;
    private VertexBuffer? _vertexBuffer;
    private byte[] _indexData = [];
    private int _indexBufferSize;
    private IndexBuffer? _indexBuffer;
    private readonly Dictionary<IntPtr, Texture2D> _loadedTextures = new();
    private int _textureId;
    private IntPtr _fontTextureId;
    private int _scrollWheelValue;
    private static readonly (ImGuiKey ImGui, Keys Xna)[] KeyMap =
    [
        (ImGuiKey.Tab, Keys.Tab),
        (ImGuiKey.LeftArrow, Keys.Left),
        (ImGuiKey.RightArrow, Keys.Right),
        (ImGuiKey.UpArrow, Keys.Up),
        (ImGuiKey.DownArrow, Keys.Down),
        (ImGuiKey.PageUp, Keys.PageUp),
        (ImGuiKey.PageDown, Keys.PageDown),
        (ImGuiKey.Home, Keys.Home),
        (ImGuiKey.End, Keys.End),
        (ImGuiKey.Delete, Keys.Delete),
        (ImGuiKey.Backspace, Keys.Back),
        (ImGuiKey.Enter, Keys.Enter),
        (ImGuiKey.Escape, Keys.Escape),
        (ImGuiKey.Space, Keys.Space),
        (ImGuiKey.A, Keys.A),
        (ImGuiKey.C, Keys.C),
        (ImGuiKey.V, Keys.V),
        (ImGuiKey.X, Keys.X),
        (ImGuiKey.Y, Keys.Y),
        (ImGuiKey.Z, Keys.Z)
    ];

    public ImGuiRenderer(Game game)
    {
        _game = game;
        _graphicsDevice = game.GraphicsDevice;
        _effect = new BasicEffect(_graphicsDevice);

        var context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);
        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

        ApplyEditorStyle();
        RebuildFontAtlas();
    }

    private static unsafe void ApplyEditorStyle()
    {
        var style = ImGui.GetStyle();
        style.WindowPadding = new Num.Vector2(18, 16);
        style.FramePadding = new Num.Vector2(14, 10);
        style.ItemSpacing = new Num.Vector2(12, 10);
        style.ItemInnerSpacing = new Num.Vector2(10, 8);
        style.ScrollbarSize = 18;
        style.GrabMinSize = 16;
        style.WindowRounding = 6;
        style.FrameRounding = 5;
        style.GrabRounding = 4;
        style.ScaleAllSizes(1.25f);

        var io = ImGui.GetIO();
        io.Fonts.Clear();
        var cfg = ImGuiNative.ImFontConfig_ImFontConfig();
        cfg->SizePixels = 22;
        cfg->OversampleH = 2;
        cfg->OversampleV = 2;
        cfg->PixelSnapH = 1;
        io.Fonts.AddFontDefault(cfg);
        ImGuiNative.ImFontConfig_destroy(cfg);
    }

    public unsafe void RebuildFontAtlas()
    {
        var io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out byte* pixelData, out var width, out var height, out var bytesPerPixel);

        var pixels = new byte[width * height * bytesPerPixel];
        Marshal.Copy(new IntPtr(pixelData), pixels, 0, pixels.Length);

        var texture = new Texture2D(_graphicsDevice, width, height, false, SurfaceFormat.Color);
        texture.SetData(pixels);

        if (_fontTextureId != IntPtr.Zero)
        {
            UnbindTexture(_fontTextureId);
        }

        _fontTextureId = BindTexture(texture);
        io.Fonts.SetTexID(_fontTextureId);
        io.Fonts.ClearTexData();
    }

    public IntPtr BindTexture(Texture2D texture)
    {
        var id = new IntPtr(_textureId++);
        _loadedTextures.Add(id, texture);
        return id;
    }

    public void UnbindTexture(IntPtr textureId)
    {
        _loadedTextures.Remove(textureId);
    }

    public void BeginLayout(GameTime gameTime)
    {
        ImGui.GetIO().DeltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        UpdateInput();
        ImGui.NewFrame();
    }

    public void EndLayout()
    {
        ImGui.Render();
        RenderDrawData(ImGui.GetDrawData());
    }

    private void UpdateInput()
    {
        if (!_game.IsActive)
        {
            return;
        }

        var io = ImGui.GetIO();
        var mouse = Mouse.GetState();
        var keyboard = Keyboard.GetState();
        io.AddMousePosEvent(mouse.X, mouse.Y);
        io.AddMouseButtonEvent(0, mouse.LeftButton == ButtonState.Pressed);
        io.AddMouseButtonEvent(1, mouse.RightButton == ButtonState.Pressed);
        io.AddMouseButtonEvent(2, mouse.MiddleButton == ButtonState.Pressed);

        var scrollDelta = mouse.ScrollWheelValue - _scrollWheelValue;
        io.AddMouseWheelEvent(0, scrollDelta / 120f);
        _scrollWheelValue = mouse.ScrollWheelValue;

        foreach (var (imgui, xna) in KeyMap)
        {
            io.AddKeyEvent(imgui, keyboard.IsKeyDown(xna));
        }

        io.AddKeyEvent(ImGuiKey.ModCtrl, keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl));
        io.AddKeyEvent(ImGuiKey.ModShift, keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift));
        io.AddKeyEvent(ImGuiKey.ModAlt, keyboard.IsKeyDown(Keys.LeftAlt) || keyboard.IsKeyDown(Keys.RightAlt));

        io.DisplaySize = new Num.Vector2(
            _graphicsDevice.PresentationParameters.BackBufferWidth,
            _graphicsDevice.PresentationParameters.BackBufferHeight);
        io.DisplayFramebufferScale = Num.Vector2.One;
    }

    private void RenderDrawData(ImDrawDataPtr drawData)
    {
        var io = ImGui.GetIO();
        drawData.ScaleClipRects(io.DisplayFramebufferScale);
        UpdateBuffers(drawData);
        RenderCommandLists(drawData);
    }

    private unsafe void UpdateBuffers(ImDrawDataPtr drawData)
    {
        if (drawData.TotalVtxCount == 0)
        {
            return;
        }

        if (drawData.TotalVtxCount > _vertexBufferSize)
        {
            _vertexBuffer?.Dispose();
            _vertexBufferSize = (int)(drawData.TotalVtxCount * 1.5f);
            _vertexBuffer = new VertexBuffer(_graphicsDevice, DrawVertDeclaration.Declaration, _vertexBufferSize, BufferUsage.None);
            _vertexData = new byte[_vertexBufferSize * DrawVertDeclaration.Size];
        }

        if (drawData.TotalIdxCount > _indexBufferSize)
        {
            _indexBuffer?.Dispose();
            _indexBufferSize = (int)(drawData.TotalIdxCount * 1.5f);
            _indexBuffer = new IndexBuffer(_graphicsDevice, IndexElementSize.SixteenBits, _indexBufferSize, BufferUsage.None);
            _indexData = new byte[_indexBufferSize * sizeof(ushort)];
        }

        var vtxOffset = 0;
        var idxOffset = 0;
        for (var n = 0; n < drawData.CmdListsCount; n++)
        {
            var cmdList = drawData.CmdLists[n];
            var vtxSize = cmdList.VtxBuffer.Size * DrawVertDeclaration.Size;
            Marshal.Copy(cmdList.VtxBuffer.Data, _vertexData, vtxOffset, vtxSize);
            vtxOffset += vtxSize;

            var idxSize = cmdList.IdxBuffer.Size * sizeof(ushort);
            Marshal.Copy(cmdList.IdxBuffer.Data, _indexData, idxOffset, idxSize);
            idxOffset += idxSize;
        }

        _vertexBuffer!.SetData(_vertexData, 0, vtxOffset);
        _indexBuffer!.SetData(_indexData, 0, idxOffset);
    }

    private void RenderCommandLists(ImDrawDataPtr drawData)
    {
        _graphicsDevice.SetVertexBuffer(_vertexBuffer);
        _graphicsDevice.Indices = _indexBuffer;
        _graphicsDevice.BlendFactor = Color.White;
        _graphicsDevice.BlendState = BlendState.NonPremultiplied;
        _graphicsDevice.RasterizerState = _rasterizerState;
        _graphicsDevice.DepthStencilState = DepthStencilState.None;
        _graphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;

        _effect.World = Matrix.Identity;
        _effect.View = Matrix.Identity;
        _effect.Projection = Matrix.CreateOrthographicOffCenter(
            0f, ioWidth(), ioHeight(), 0f, -1f, 1f);
        _effect.TextureEnabled = true;
        _effect.VertexColorEnabled = true;

        var vtxOffset = 0;
        var idxOffset = 0;
        for (var n = 0; n < drawData.CmdListsCount; n++)
        {
            var cmdList = drawData.CmdLists[n];
            for (var cmdi = 0; cmdi < cmdList.CmdBuffer.Size; cmdi++)
            {
                var drawCmd = cmdList.CmdBuffer[cmdi];
                if (drawCmd.ElemCount == 0)
                {
                    continue;
                }

                if (!_loadedTextures.ContainsKey(drawCmd.TextureId))
                {
                    continue;
                }

                _graphicsDevice.ScissorRectangle = new Rectangle(
                    (int)drawCmd.ClipRect.X,
                    (int)drawCmd.ClipRect.Y,
                    (int)(drawCmd.ClipRect.Z - drawCmd.ClipRect.X),
                    (int)(drawCmd.ClipRect.W - drawCmd.ClipRect.Y));

                _effect.Texture = _loadedTextures[drawCmd.TextureId];
                foreach (var pass in _effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    _graphicsDevice.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        (int)drawCmd.VtxOffset + vtxOffset,
                        (int)drawCmd.IdxOffset + idxOffset,
                        (int)drawCmd.ElemCount / 3);
                }
            }

            vtxOffset += cmdList.VtxBuffer.Size;
            idxOffset += cmdList.IdxBuffer.Size;
        }

        float ioWidth() => ImGui.GetIO().DisplaySize.X;
        float ioHeight() => ImGui.GetIO().DisplaySize.Y;
    }

    public void Dispose()
    {
        _vertexBuffer?.Dispose();
        _indexBuffer?.Dispose();
        _effect.Dispose();
        _rasterizerState.Dispose();
    }

    private static class DrawVertDeclaration
    {
        public static readonly VertexDeclaration Declaration = new(
            new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
            new VertexElement(8, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
            new VertexElement(16, VertexElementFormat.Color, VertexElementUsage.Color, 0));

        public static readonly int Size = Declaration.VertexStride;
    }
}
