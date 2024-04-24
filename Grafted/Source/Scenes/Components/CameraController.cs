namespace Grafted.Scenes.Components;

public class CameraController {
    public readonly Camera Camera;

    public Vector2 MouseWorldPosition {
        get {
            MouseState mouseState = Mouse.GetState();
            return Camera.ScreenToWorld(new Vector2(mouseState.X, mouseState.Y));
        }
    }

    public CameraController(Camera camera) {
        Camera = camera;
    }

    /*public Vector2Int MouseCell() {
        (float x, float y) = ConvertUnits.ToSimUnits(MouseWorldPosition);
        return new Vector2Int((int) x, (int) y);
    }*/

    public void Update(float deltaTime) {
        Camera.UpdateMouseXY();
        if (Input.IsKeyDown(Keys.Q)) {
            Camera.Scale += new Vector2(deltaTime, deltaTime);
        }

        if (Input.IsKeyDown(Keys.E)) {
            Camera.Scale -= new Vector2(deltaTime, deltaTime);
        }

        var moveSpeed = 500;
        if (Input.IsKeyDown(Keys.A)) {
            Camera.X -= deltaTime * moveSpeed;
        }

        if (Input.IsKeyDown(Keys.D)) {
            Camera.X += deltaTime * moveSpeed;
        }

        if (Input.IsKeyDown(Keys.W)) {
            Camera.Y -= deltaTime * moveSpeed;
        }

        if (Input.IsKeyDown(Keys.S)) {
            Camera.Y += deltaTime * moveSpeed;
        }
        // (float x, float y) = Core.Sim.Player.DrawPosition;
        // _camera.X += (x - _camera.X) / 2;
        // _camera.Y += (y - _camera.Y) / 20;
    }
}