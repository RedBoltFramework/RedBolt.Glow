// Copyright (c) 2026 RedBolt and contributors.
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT> or the accompanying LICENSE file.

namespace RedBolt.Sample.BakedLighting;

using System;

using RedBolt;
using RedBolt.Bindings.DearImGui;
using RedBolt.DearImGui;
using RedBolt.Core;
using RedBolt.Engine;
using RedBolt.Graphics;
using RedBolt.Input;
using RedBolt.Math;
using RedBolt.Resource;
using RedBolt.Scene;
using Widgets = RedBolt.SystemUI.Widgets;

internal static class BakedLightingProgram
{
    private static int Main()
    {
        try
        {
            using var application = new BakedLighting();
            var exitCode = application.Run();
            if (exitCode == 0)
                Console.WriteLine("RedBolt BakedLighting sample finished.");
            return exitCode;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }
}

/// <summary>Baked lighting example.</summary>
public class BakedLighting : Application
{
    /// <summary>Construct.</summary>
    public BakedLighting()
    {
    }

    /// <summary>Setup after engine initialization and before running the main loop.</summary>
    protected override void Start()
    {
        // Execute base class startup
        base.Start();
        _sampleInput = new SampleInputMap(movement: true);
        _sampleInput.AddButton("Boost", Scancode.LeftShift, ControllerButton.RightShoulder, "RUN");
        _sampleInput.Map.MapKeyboardKey("Boost", Scancode.RightShift);
        _sampleInput.AddButton("Textures", Scancode.Tab, ControllerButton.Y, "TEXTURE", 1);
        _sampleInput.Map.MapControllerAxis("LookX", ControllerAxis.RightX, 0.0f, 1.0f);
        _sampleInput.Map.MapControllerAxis("LookY", ControllerAxis.RightY, 0.0f, 1.0f);
        // Create the UI content
        CreateInstructions();

        // Create scene content after Dear ImGui so its render pass stays on top.
        CreateScene();

        // Subscribe to necessary events
        SubscribeToEvents();

        // Set the mouse mode to use in the sample
        Input.SetMouseMode(MouseMode.Relative);
        Input.SetMouseVisible(false);
    }

    /// <summary>Create scene content.</summary>
    private void CreateScene()
    {
        var cache = Resources;

        // Load scene
        _scene = new Scene();

        var xmlFile = cache.Get<XMLFile>("Scenes/BakedLightingExample.xml");
        using var root = xmlFile.GetRoot();
        _scene.LoadXML(root);

        var camera = _scene.FindComponent<Camera>()!;
        _cameraNode = camera.Node!;
        _cameraNode.SetPosition(new Vector3f(0.0f, 0.0f, -8.0f));
        _cameraDistance = 8.0f;
        _viewport = new Viewport(_scene, camera);
        Renderer.SetViewport(0, _viewport);

        _actorNode = _scene.FindChild("Actor")!;

        var animController = _actorNode.FindComponent<AnimationController>()!;
        animController.PlayNewExclusive(new AnimationParameters(cache.Get<Animation>("Models/Mutant/Mutant_Idle0.ani")).Looped());

        // Initialize yaw and pitch angles
        _cameraRotationPitch = _cameraNode.Parent!;
        _cameraRotationYaw = _cameraRotationPitch.Parent!;
        _yaw = _cameraRotationYaw.WorldRotation.YawAngle;
        _pitch = _cameraRotationPitch.WorldRotation.PitchAngle;
    }

    /// <summary>Construct an instruction text to the UI.</summary>
    private void CreateInstructions()
    {
        Context.RegisterSubsystem(new DearImGui((ImGuiConfigFlags)Engine.Parameters.DearImGuiFlags));
        SubscribeToEvent<DearImGuiEvents.EndRenderingDearImGuiEvent>(
            (ref DearImGuiEvents.EndRenderingDearImGuiEvent _) => DrawInstructions());

        _controlPrompts =
        [
            Resources.Get<Texture2D>("ThirdParty/KenneyInputPrompts/keyboard_w.png"),
            Resources.Get<Texture2D>("ThirdParty/KenneyInputPrompts/keyboard_a.png"),
            Resources.Get<Texture2D>("ThirdParty/KenneyInputPrompts/keyboard_s.png"),
            Resources.Get<Texture2D>("ThirdParty/KenneyInputPrompts/keyboard_d.png"),
            Resources.Get<Texture2D>("ThirdParty/KenneyInputPrompts/mouse_move.png"),
            Resources.Get<Texture2D>("ThirdParty/KenneyInputPrompts/keyboard_shift.png"),
            Resources.Get<Texture2D>("ThirdParty/KenneyInputPrompts/keyboard_tab.png")
        ];

        // Construct new Text object, set string to display and font to use
        // The text has multiple rows. Center them in relation to each other
        // Position the text relative to the screen center
        // The rbfx Text control is replaced by the allowed Dear ImGui sample UI surface.
    }

    /// <summary>Subscribe to necessary events.</summary>
    private void SubscribeToEvents()
    {
        // Unsubscribe the SceneUpdate event from base class as the camera node is being controlled in HandlePostUpdate() in this sample
        SubscribeToEvent<InputEvents.MouseWheelEvent>(HandleMouseWheel);
        _multitouchAdapter = new MultitouchAdapter();
        _multitouchAdapter.SetEnabled(true);
        SubscribeToEvent<InputEvents.MultitouchEvent>(_multitouchAdapter, HandleMultitouch);
    }

    private void HandleMouseWheel(ref InputEvents.MouseWheelEvent eventData)
    {
        ZoomCamera(eventData.Wheel * 0.75f);
    }

    private void HandleMultitouch(ref InputEvents.MultitouchEvent eventData)
    {
        if ((MultitouchEventType)eventData.EventType != MultitouchEventType.MultitouchMove)
            return;

        if (eventData.NumFingers == 1 && eventData.X >= Graphics.Width / 2)
        {
            var sensitivity = 90.0f / Math.Max(1, Graphics.Height);
            _yaw = MathDefs.Mod(_yaw - eventData.DX * sensitivity, 360.0f);
            _pitch = MathDefs.Clamp(_pitch - eventData.DY * sensitivity, -89.0f, 89.0f);
        }
        else if (eventData.NumFingers == 2)
            ZoomCamera((eventData.DSize.X + eventData.DSize.Y) * 0.01f);
    }

    private void ZoomCamera(float amount)
    {
        _cameraDistance = MathDefs.Clamp(_cameraDistance - amount, 3.0f, 14.0f);
        _cameraNode?.SetPosition(new Vector3f(0.0f, 0.0f, -_cameraDistance));
    }

    /// <summary>Handle application update. Set controls to character.</summary>
    protected override void Update(float timeStep)
    {
        if (Input.ExitRequested || _sampleInput!.Pressed("Exit"))
        {
            Exit();
            return;
        }

        var cache = Resources;

        // Update camera rotation
        const float rotationSensitivity = 0.1f;
        var mouseMove = Input.GetMouseMove();
        _yaw = MathDefs.Mod(_yaw + mouseMove.X * rotationSensitivity +
            _sampleInput.Evaluate("LookX") * timeStep * 120.0f, 360.0f);
        _pitch = MathDefs.Clamp(_pitch + mouseMove.Y * rotationSensitivity +
            _sampleInput.Evaluate("LookY") * timeStep * 120.0f, -89.0f, 89.0f);

        _cameraRotationPitch!.SetRotation(new Quaternion(_pitch, 0.0f, 0.0f));
        _cameraRotationYaw!.SetRotation(new Quaternion(0.0f, _yaw, 0.0f));

        // Apply movement
        var rotation = _cameraRotationYaw!.WorldRotation;
        var controlDirection = Vector3f.Zero;
        controlDirection += Vector3f.Forward * _sampleInput.Evaluate("Forward");
        controlDirection += Vector3f.Back * _sampleInput.Evaluate("Back");
        controlDirection += Vector3f.Left * _sampleInput.Evaluate("Left");
        controlDirection += Vector3f.Right * _sampleInput.Evaluate("Right");

        var movementDirection = rotation * controlDirection;
        var movementSpeed = _sampleInput.Evaluate("Boost") > 0.0f ? 5.0f : 2.0f;
        var velocity = movementSpeed * movementDirection;
        _actorNode!.Translate(velocity * timeStep, TransformSpace.World);

        // Animate model
        var runAnimation = cache.Get<Animation>("Models/Mutant/Mutant_Run.ani");
        var idleAnimation = cache.Get<Animation>("Models/Mutant/Mutant_Idle0.ani");

        var animController = _actorNode.FindComponent<AnimationController>()!;
        var rotationNode = animController.Node.Parent!;
        var actualVelocityFlat = velocity * new Vector3f(1.0f, 0.0f, 1.0f);
        if (actualVelocityFlat.Length > MathDefs.LargeEpsilon)
        {
            var animationSpeed = actualVelocityFlat.Length * 0.3f;
            rotationNode.SetWorldDirection(actualVelocityFlat);
            animController.PlayExistingExclusive(new AnimationParameters(runAnimation).Looped().Speed(animationSpeed), 0.2f);
        }
        else
            animController.PlayExistingExclusive(new AnimationParameters(idleAnimation).Looped(), 0.2f);

        // Toggle textures
        if (_sampleInput.Pressed("Textures"))
            ToggleTextures();
    }

    private void ToggleTextures()
    {
        var animModel = _actorNode?.GetComponent<AnimatedModel>();
        if (animModel is null)
            return;
        _texturesEnabled = !_texturesEnabled;
        animModel.SetMaterial(_texturesEnabled
            ? Resources.Get<Material>("Models/Mutant/Materials/mutant_M.xml")
            : Resources.Get<Material>("Materials/DefaultWhite.xml"));
    }

    protected override void Stop()
    {
        _sampleInput?.Dispose();
        _sampleInput = null;
        Renderer.SetNumViewports(0);
        _viewport?.Dispose();
        _viewport = null;
        _cameraNode = null;
        _actorNode = null;
        _scene?.Dispose();
        _scene = null;
        _multitouchAdapter?.Dispose();
        _multitouchAdapter = null;
        base.Stop();
    }

    private void DrawInstructions()
    {
        var windowOpen = true;
        ImGui.SetNextWindowPos(new Vector2f(10.0f, 10.0f), ImGuiCond.ImGuiCondAlways);
        ImGui.SetNextWindowSize(new Vector2f(305.0f, 205.0f), ImGuiCond.ImGuiCondAlways);
        if (ImGui.Begin("Baked Lighting"u8, ref windowOpen,
            ImGuiWindowFlags.ImGuiWindowFlagsNoResize | ImGuiWindowFlags.ImGuiWindowFlagsNoSavedSettings))
        {
            for (var i = 0; i < 3; ++i)
            {
                ImGui.Image(_controlPrompts![i], PromptIconSize);
                ImGui.SameLine();
            }
            DrawPrompt(_controlPrompts![3], "Move"u8);
            DrawPrompt(_controlPrompts[4], "Look / wheel zoom"u8);
            DrawPrompt(_controlPrompts[5], "Sprint"u8);
            DrawPrompt(_controlPrompts[6], "Toggle character textures"u8);
            if (ImGui.Button("Zoom in"u8))
                ZoomCamera(0.75f);
            ImGui.SameLine();
            if (ImGui.Button("Zoom out"u8))
                ZoomCamera(-0.75f);
            ImGui.SameLine();
            if (ImGui.Button("Textures"u8))
                ToggleTextures();
        }
        ImGui.End();
    }

    private static void DrawPrompt(Texture2D texture, ReadOnlySpan<byte> label)
    {
        ImGui.Image(texture, PromptIconSize);
        ImGui.SameLine();
        Widgets.AlignTextToLastItem();
        ImGui.TextUnformatted(label);
    }

    protected override void Setup(EngineParameters parameters)
    {
        parameters.ApplicationName = "RedBolt.Sample.BakedLighting";
        parameters.Borderless = false;
        parameters.FullScreen = false;
        parameters.ResourcePaths = "CoreData.Glow;SampleData.Glow;CoreData;SampleData";
        parameters.ResourcePrefixPaths = ".;..";
        parameters.WindowMaximize = false;
#if !REDBOLT_PLATFORM_BROWSER
            parameters.WindowHeight = 720;
            parameters.WindowWidth = 1280;
#endif
        parameters.WindowTitle = "RedBolt Baked Lighting";
    }

    public override void Dispose()
    {
        if (_scene is not null)
            Renderer.SetNumViewports(0);
        _viewport?.Dispose();
        _viewport = null;
        _cameraNode = null;
        _actorNode = null;
        _scene?.Dispose();
        _scene = null;
        base.Dispose();
    }

    private Scene? _scene;
    private Node? _cameraNode;
    private Node? _actorNode;
    private Node? _cameraRotationPitch;
    private Node? _cameraRotationYaw;
    private Viewport? _viewport;
    private float _yaw;
    private float _pitch;
    private float _cameraDistance;
    private bool _texturesEnabled = true;
    private Texture2D[]? _controlPrompts;
    private MultitouchAdapter? _multitouchAdapter;
    private SampleInputMap? _sampleInput;
    private static readonly Vector2f PromptIconSize = new(24.0f, 24.0f);
}
