using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class KeyboardManager : MonoBehaviour
{
    public static KeyboardManager Instance { get; private set; }  // Instancia del propio objeto para comunicarse con otros scripts
    public Vector2 movimientoCamara { get; private set; } // Indica el movimiento que hace la cámara al pulsar las flechas en la visión de tablero activada

    [Header("Botones de la UI")]
    [SerializeField] private Button botonTurnCards;
    [SerializeField] private Button botonUseEnergy;
    [SerializeField] private Button botonMonsterBack;
    [SerializeField] private Button botonMonsterMove;
    [SerializeField] private Button botonMonsterAttack;
    [SerializeField] private Button botonMonsterHability;
    [SerializeField] private Button botonStructureBack;
    [SerializeField] private Button botonStructureAttack;
    [SerializeField] private Button botonStructureHability;
    [SerializeField] private Button botonTemplateBack;
    [SerializeField] private Button botonPauseContinue;
    [SerializeField] private Button botonConfirmBack;
    [SerializeField] private Button botonConfirmConfirm;
    [SerializeField] private Button botonMenuBack;
    [SerializeField] private Button botonMenuNextTurn;
    [SerializeField] private Button botonDeckBack;
    [SerializeField] private Button botonDeckDraw;
    [SerializeField] private Button botonResetCamera;
    [SerializeField] private Button botonBoardView;

    [Header("Teclas asignadas")]
    private Key keyTurnCards = Key.G;
    private Key keyUseEnergy = Key.U;
    private Key keyMonsterBack = Key.V;
    private Key keyMonsterMove = Key.M;
    private Key keyMonsterAttack = Key.A;
    private Key keyMonsterHability = Key.H;
    private Key keyStructureBack = Key.V;
    private Key keyStructureAttack = Key.A;
    private Key keyStructureHability = Key.H;
    private Key keyTemplateBack = Key.V;
    private Key keyPauseContinue = Key.C;
    private Key keyConfirmBack = Key.V;
    private Key keyConfirmConfirm = Key.C;
    private Key keyMenuBack = Key.V;
    private Key keyMenuNextTurn = Key.P;
    private Key keyResetCamera = Key.P;
    private Key keyDeckBack = Key.V;
    private Key keyDeckDraw = Key.R;
    private Key keyBoardView = Key.O;

    [Header("Teclas para acceso rápido a Deck y Menu")]
    private Key keyAccessDeck = Key.R;
    private Key keyAccessMenu = Key.E;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (Keyboard.current == null)
            return;

        Vector2 movimiento = Vector2.zero;

        if (Keyboard.current.upArrowKey.isPressed || (Keyboard.current.wKey != null && Keyboard.current.wKey.isPressed))
            movimiento.y += 1f;

        if (Keyboard.current.downArrowKey.isPressed || (Keyboard.current.sKey != null && Keyboard.current.sKey.isPressed))
            movimiento.y -= 1f;

        if (Keyboard.current.leftArrowKey.isPressed || (Keyboard.current.aKey != null && Keyboard.current.aKey.isPressed))
            movimiento.x -= 1f;

        if (Keyboard.current.rightArrowKey.isPressed || (Keyboard.current.dKey != null && Keyboard.current.dKey.isPressed))
            movimiento.x += 1f;

        movimientoCamara = movimiento;

        if (Keyboard.current[keyTurnCards].wasPressedThisFrame)
            InvokeButton(botonTurnCards);

        if (Keyboard.current[keyUseEnergy].wasPressedThisFrame)
            InvokeButton(botonUseEnergy);

        if (Keyboard.current[keyMonsterBack].wasPressedThisFrame)
            InvokeButton(botonMonsterBack);

        if (Keyboard.current[keyMonsterMove].wasPressedThisFrame)
            InvokeButton(botonMonsterMove);

        if (Keyboard.current[keyMonsterAttack].wasPressedThisFrame)
            InvokeButton(botonMonsterAttack);

        if (Keyboard.current[keyMonsterHability].wasPressedThisFrame)
            InvokeButton(botonMonsterHability);

        if (Keyboard.current[keyStructureBack].wasPressedThisFrame)
            InvokeButton(botonStructureBack);

        if (Keyboard.current[keyStructureAttack].wasPressedThisFrame)
            InvokeButton(botonStructureAttack);

        if (Keyboard.current[keyStructureHability].wasPressedThisFrame)
            InvokeButton(botonStructureHability);

        if (Keyboard.current[keyTemplateBack].wasPressedThisFrame)
            InvokeButton(botonTemplateBack);

        if (Keyboard.current[keyPauseContinue].wasPressedThisFrame)
            InvokeButton(botonPauseContinue);

        if (Keyboard.current[keyConfirmBack].wasPressedThisFrame)
            InvokeButton(botonConfirmBack);

        if (Keyboard.current[keyConfirmConfirm].wasPressedThisFrame)
            InvokeButton(botonConfirmConfirm);

        if (Keyboard.current[keyMenuBack].wasPressedThisFrame)
            InvokeButton(botonMenuBack);

        if (Keyboard.current[keyMenuNextTurn].wasPressedThisFrame)
            InvokeButton(botonMenuNextTurn);

        if (Keyboard.current[keyDeckBack].wasPressedThisFrame)
            InvokeButton(botonDeckBack);

        if (Keyboard.current[keyDeckDraw].wasPressedThisFrame)
            InvokeButton(botonDeckDraw);

        if (Keyboard.current[keyResetCamera].wasPressedThisFrame)
            InvokeButton(botonResetCamera);

        if (Keyboard.current[keyBoardView].wasPressedThisFrame)
            InvokeButton(botonBoardView);

        // Acceso rápido a Deck y Menu
        if (Keyboard.current[keyAccessDeck].wasPressedThisFrame)
            MoverAlDeck();

        if (Keyboard.current[keyAccessMenu].wasPressedThisFrame)
            MoverAlMenu();
    }

    private void MoverAlDeck()
    {
        if (CameraController.Instance != null)
            CameraController.Instance.MoverAlDeck();
    }

    private void MoverAlMenu()
    {
        if (CameraController.Instance != null)
            CameraController.Instance.MoverAlMenu();
    }

    private void InvokeButton(Button button)
    {
        if (button == null || !button.gameObject.activeInHierarchy || !button.interactable)
            return;

        button.onClick.Invoke();
    }
}
