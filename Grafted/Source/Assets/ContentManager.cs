using System;
using System.Collections.Generic;
using System.IO;
using Grafted.Assets.SpriteAtlases;
using Grafted.Assets.SpriteAtlases.Loader;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Grafted.Assets;

public class ContentManager {
    private readonly GraphicsDevice _graphicsDevice;
    public readonly string RootDirectory = string.Empty;

    private readonly List<IDisposable> _disposableAssets;
    private readonly Dictionary<string, object?> _loadedAssets = new(StringComparer.OrdinalIgnoreCase);

    public ContentManager(GraphicsDevice graphicsDevice, string rootDirectory) {
        _graphicsDevice = graphicsDevice;
        RootDirectory = rootDirectory;
        _disposableAssets = new List<IDisposable>();
    }

    public T Load<T>(string assetName) where T : class {
        if (_loadedAssets.TryGetValue(assetName, out object? a) && a is T preLoadedAsset) {
            return preLoadedAsset;
        }

        T asset = ReadAsset<T>(assetName)!;
        _loadedAssets.Add(assetName, asset);
        return asset;
    }

    private T? ReadAsset<T>(string assetName) where T : class {
        T? asset = default;

        if (typeof(T) == typeof(Texture2D)) {
            return LoadTexture2D(assetName) as T;
        }

        if (typeof(T) == typeof(SpriteAtlas)) {
            return LoadSpriteAtlas(assetName) as T;
        }

        if (typeof(T) == typeof(StreamReader)) {
            return LoadTextFile(assetName) as T;
        }

        if (asset is GraphicsResource graphicsResource) {
            graphicsResource.Name = assetName;
        }

        if (asset is IDisposable disposable) {
            _disposableAssets.Add(disposable);
        }

        return asset ?? throw new ContentLoadException("Could not load " + assetName + " asset!");
    }

    private Texture2D LoadTexture2D(string assetName) {
        try {
            return Texture2D.FromFile(_graphicsDevice, $"{RootDirectory}/Textures/{assetName}.png");
        }
        catch (Exception e) {
            if (e is DirectoryNotFoundException or FileNotFoundException) {
                return Texture2D.FromFile(_graphicsDevice, $"{RootDirectory}/Textures/{assetName}");
            }

            throw;
        }
    }

    /// <summary>
    /// Loads a SpriteAtlas created with the Sprite Atlas Packer tool
    /// </summary>
    public SpriteAtlas LoadSpriteAtlas(string assetName, bool premultiplyAlpha = false) {
        return SpriteAtlasLoader.ParseSpriteAtlas($"{RootDirectory}/Textures/{assetName}", premultiplyAlpha);
    }

    public StreamReader LoadTextFile(string assetName) {
        return File.OpenText($"{RootDirectory}/Data/{assetName}.txt");
    }

    /// <summary>
    ///  will load embedded resources if they have the "ash://" prefix
    /// </summary>
    protected Stream? OpenStream(string assetName) {
        return GetType().Assembly.GetManifestResourceStream(assetName);
    }

    public void Unload() {
        foreach (IDisposable disposableAsset in _disposableAssets) {
            disposableAsset.Dispose();
        }

        _disposableAssets.Clear();
        _loadedAssets.Clear();
    }
}