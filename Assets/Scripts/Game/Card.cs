using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public enum CardType
{
    Monstruo,
    Estructura,
    Hechizo,
    Trampa,
    MonstruoLeg,
    Energia
}

[System.Serializable]
public class CardData
{
    public string nombre;
    public CardType tipo;
    public int costoEnergia;
    public Sprite imagenCarta;
    public Image imageUI; // Para mostrar la imagen de la carta en la UI
    
    public int costeHabilidad = 0;

    public virtual CardData Clone()
    {
        return new CardData
        {
            nombre = this.nombre,
            tipo = this.tipo,
            costoEnergia = this.costoEnergia,
            imagenCarta = this.imagenCarta,
            imageUI = this.imageUI
        };
    }
}

[System.Serializable]
public class SpellCardData : CardData
{
    public bool actuaEnArea; // Indica si el hechizo actúa en un área
    public int radioArea; // Radio del área
    public bool requiereMonstruo; // Indica si el hechizo requiere seleccionar un monstruo enemigo

    public override CardData Clone()
    {
        return new SpellCardData
        {
            nombre = this.nombre,
            tipo = this.tipo,
            costoEnergia = this.costoEnergia,
            imagenCarta = this.imagenCarta,
            imageUI = this.imageUI,
            actuaEnArea = this.actuaEnArea,
            radioArea = this.radioArea,
            requiereMonstruo = this.requiereMonstruo
        };
    }
}

[System.Serializable]
public class DanyoEfecto
{ // Para efectos que hacen daño durante varios turnos
    public string nombre; // Nombre del efecto
    public int danyo; // Daño que se hace por turno
    public int turnosRestantes; // Turnos restantes para que el efecto termine (-1 para infinito)

    public DanyoEfecto(string nombre, int danyo, int turnos)
    {
        this.nombre = nombre;
        this.danyo = danyo;
        this.turnosRestantes = turnos;
    }

    public DanyoEfecto Clone()
    {
        return new DanyoEfecto(nombre, danyo, turnosRestantes);
    }
}

public class DamageableCardData : CardData
{
    public int vida; // Vida actual de la carta
    public int vidaMaxima;
    public int ataque;
    public int alcance;
    public List<DanyoEfecto> efectosDanyo = new();

    public override CardData Clone()
    {
        return new DamageableCardData
        {
            nombre = this.nombre,
            tipo = this.tipo,
            costoEnergia = this.costoEnergia,
            imagenCarta = this.imagenCarta,
            imageUI = this.imageUI,
            vida = this.vida,
            vidaMaxima = this.vidaMaxima,
            ataque = this.ataque,
            alcance = this.alcance,
            efectosDanyo = this.efectosDanyo.ConvertAll(e => e.Clone())
        };
    }
}

[System.Serializable]
public class MonsterCardData : DamageableCardData
{
    public int velocidad;

    public override CardData Clone()
    {
        return new MonsterCardData
        {
            nombre = this.nombre,
            tipo = this.tipo,
            costoEnergia = this.costoEnergia,
            imagenCarta = this.imagenCarta,
            imageUI = this.imageUI,
            vida = this.vida,
            vidaMaxima = this.vidaMaxima,
            ataque = this.ataque,
            alcance = this.alcance,
            velocidad = this.velocidad,
            costeHabilidad = this.costeHabilidad
        };
    }
}

[System.Serializable]
public class StructureCardData : DamageableCardData
{
    public override CardData Clone()
    {
        return new StructureCardData
        {
            nombre = this.nombre,
            tipo = this.tipo,
            costoEnergia = this.costoEnergia,
            imagenCarta = this.imagenCarta,
            imageUI = this.imageUI,
            vida = this.vida,
            vidaMaxima = this.vidaMaxima,
            ataque = this.ataque,
            alcance = this.alcance,
            costeHabilidad = this.costeHabilidad
        };
    }
}

[System.Serializable]
public class TrapCardData : CardData
{
    public int ataque;
    public int turnos;
    public int turnosMaximos;

    public override CardData Clone()
    {
        return new TrapCardData
        {
            nombre = this.nombre,
            tipo = this.tipo,
            costoEnergia = this.costoEnergia,
            imagenCarta = this.imagenCarta,
            imageUI = this.imageUI,
            costeHabilidad = this.costeHabilidad,
            ataque = this.ataque,
            turnos = this.turnos,
            turnosMaximos = this.turnosMaximos
        };
    }
}

public class Card : MonoBehaviour
{
    public CardData cardData; // Información de la carta
    public Cell casilla; // Casilla en la que está colocada la carta
    public ClickableObject clickableObject; // Para establecer la carta como clickable
    [SerializeField] private TextMeshPro textoVida; // Indica la vida actual de la carta
    [SerializeField] private TextMeshPro textoVelocidad; // Indica la velocidad de la carta (para monstruos)
    [SerializeField] private TextMeshPro textoAtaqueMonstruo; // Indica el ataque de la carta (para monstruos)
    [SerializeField] private TextMeshPro textoAtaqueEstructura; // Indica el ataque de la carta (para estructuras)
    public GameObject background; // Fondo para que se vea bien textoVida
    public GameObject backgroundVelocidad; // Fondo para que se vea bien textoVelocidad
    public GameObject backgroundAtaqueMonstruo; // Fondo para que se vea bien textoAtaqueMonstruo
    public GameObject backgroundAtaqueEstructura; // Fondo para que se vea bien textoAtaqueEstructura
    public PassiveAbility pasiva; // Habilidad pasiva de la carta (null si no tiene)
    public ActiveAbility activa; // Habilidad activa de la carta (null si no tiene)
    
    // Estados otorgados por habilidades activas
    public bool invulnerableHastaProximoTurno = false;
    public bool inmuneHechizosIndefinido = false;

    // Modificadores temporales para el próximo ataque
    public int bonusDanyoProximoAtaque = 0;
    public int multDanyoProximoAtaque = 1;
    public bool areaProximoAtaque = false;
    public bool espiaActivoProximoAtaque = false;

    // Modificadores indefinidos
    public int multDanyoIndefinido = 1;

    // Modificadores de trampas
    public int bonusDanyoTrampa = 0;
    public int multDanyoTrampa = 1;
    public bool trampaAplicaAturdimiento = false;
    public bool trampaAplicaRalentizacion = false;
    public int trampaAplicaFuego = 0;

    // Instancia una nueva carta a partir de los datos indicados
    public void Setup(CardData data)
    {
        if (background != null) background.SetActive(false);
        if (backgroundVelocidad != null) backgroundVelocidad.SetActive(false);
        if (backgroundAtaqueMonstruo != null) backgroundAtaqueMonstruo.SetActive(false);
        if (backgroundAtaqueEstructura != null) backgroundAtaqueEstructura.SetActive(false);
        cardData = data.Clone();
        name = data.nombre;

        if (clickableObject == null)
            clickableObject = GetComponent<ClickableObject>();
        if (clickableObject != null && clickableObject.renderizador == null)
            clickableObject.renderizador = clickableObject.GetComponent<Renderer>();

        if (data.tipo != CardType.Trampa && clickableObject?.renderizador != null && data.imagenCarta != null) // Si la carta es una trampa, no se muestra su imagen
            clickableObject.renderizador.material.mainTexture = data.imagenCarta.texture;

        if (clickableObject != null)
        {
            clickableObject.tipoObjeto = data.tipo switch
            {
                CardType.Monstruo => TipoObjeto.Monstruo,
                CardType.Estructura => TipoObjeto.Estructura,
                CardType.Hechizo => TipoObjeto.Hechizo,
                CardType.Trampa => TipoObjeto.Trampa,
                CardType.MonstruoLeg => TipoObjeto.MonstruoLeg,
                CardType.Energia => TipoObjeto.Energia,
                _ => TipoObjeto.Ninguno
            };
        }
        
        RefrescarAtaqueUI();
    }

    // Actualiza y muestra la nueva vida de la carta
    public void UpdateVida(int nuevaVida)
    {
        DamageableCardData data = cardData as DamageableCardData;
        // Si la carta recibe daño y es invulnerable, ignorarlo
        if (nuevaVida < data.vida && PassiveAbility.EsInvulnerableATodo(this)) return;
        data.vida = nuevaVida;
        textoVida.text = data.vida.ToString();
        if (data.vidaMaxima == nuevaVida)
            background.SetActive(false);
        else
            background.SetActive(true);
    }

    // Limpia los estados de habilidades activas que duran hasta el próximo turno
    public void ResetBuffsTurno()
    {
        invulnerableHastaProximoTurno = false;
        multDanyoProximoAtaque = 1;
        bonusDanyoProximoAtaque = 0;
        areaProximoAtaque = false;
        espiaActivoProximoAtaque = false;
        RefrescarAtaqueUI();
    }

    // Actualiza y muestra la nueva velocidad de la carta (para monstruos)
    public void UpdateVelocidad(int nuevaVelocidad)
    {
        MonsterCardData data = cardData as MonsterCardData;
        if (data != null)
        {
            data.velocidad = nuevaVelocidad;
            if (textoVelocidad != null)
            {
                textoVelocidad.text = nuevaVelocidad.ToString();
                
                // Obtiene la velocidad original para ocultar el texto si no ha cambiado
                int velocidadOriginal = nuevaVelocidad;
                if (DeckManager.Instance != null)
                {
                    MonsterCardData originalData = DeckManager.Instance.GetCardDataByName(data.nombre) as MonsterCardData;
                    if (originalData != null)
                        velocidadOriginal = originalData.velocidad;
                }

                bool modificada = nuevaVelocidad != velocidadOriginal;
                textoVelocidad.gameObject.SetActive(modificada);
                if (backgroundVelocidad != null) backgroundVelocidad.SetActive(modificada);
            }
        }
    }

    // Actualiza y muestra el ataque calculado de todas las cartas
    public void RefrescarAtaqueUI()
    {
        if (Board.Instance == null || Board.Instance.cells == null) return;

        for (int row = 0; row < Board.Instance.rows; row++)
        {
            for (int col = 0; col < Board.Instance.columns; col++)
            {
                Cell cell = Board.Instance.cells[row, col];
                if (cell != null && cell.ocupada && cell.cartaActual != null)
                    RefrescarAtaqueUIParaCarta(cell.cartaActual);
            }
        }
    }

    private static void RefrescarAtaqueUIParaCarta(Card carta)
    {
        if (carta == null) return;

        if (carta.textoAtaqueMonstruo != null) carta.textoAtaqueMonstruo.gameObject.SetActive(false);
        if (carta.textoAtaqueEstructura != null) carta.textoAtaqueEstructura.gameObject.SetActive(false);
        if (carta.backgroundAtaqueMonstruo != null) carta.backgroundAtaqueMonstruo.SetActive(false);
        if (carta.backgroundAtaqueEstructura != null) carta.backgroundAtaqueEstructura.SetActive(false);

        DamageableCardData data = carta.cardData as DamageableCardData;
        if (data == null) return;

        bool mostrarParaMonstruo = data is MonsterCardData;
        bool mostrarParaEstructura = data is StructureCardData;
        if (!mostrarParaMonstruo && !mostrarParaEstructura) return;

        int ataqueBase = data.ataque;
        int ataqueTotal = PassiveAbility.CalcularDanyoAtacante(carta, null);
        bool ataqueModificado = ataqueTotal != ataqueBase;

        if (mostrarParaEstructura)
        {
            if (carta.textoAtaqueEstructura != null)
            {
                carta.textoAtaqueEstructura.text = ataqueTotal.ToString();
                carta.textoAtaqueEstructura.gameObject.SetActive(ataqueModificado);
            }
            if (carta.backgroundAtaqueEstructura != null && carta.textoAtaqueEstructura != null)
                carta.backgroundAtaqueEstructura.SetActive(ataqueModificado && carta.textoAtaqueEstructura.gameObject.activeSelf);
        }
        else if (mostrarParaMonstruo)
        {
            if (carta.textoAtaqueMonstruo != null)
            {
                carta.textoAtaqueMonstruo.text = ataqueTotal.ToString();
                carta.textoAtaqueMonstruo.gameObject.SetActive(ataqueModificado);
            }
            if (carta.backgroundAtaqueMonstruo != null && carta.textoAtaqueMonstruo != null)
                carta.backgroundAtaqueMonstruo.SetActive(ataqueModificado && carta.textoAtaqueMonstruo.gameObject.activeSelf);
        }
    }

    // Actualiza los efectos por turno activos
    public void UpdateEfectos()
    {
        // Si la carta es invulnerable, los efectos de daño no se aplican
        if (PassiveAbility.EsInvulnerableATodo(this)) return;

        if (cardData is DamageableCardData dData && dData.efectosDanyo.Count > 0)
        { // Si la carta tiene efectos de daño activos
            int danyoTotal = 0;
            // Usamos una lista temporal para guardar los efectos que acaban este turno
            List<DanyoEfecto> aEliminar = new();

            foreach (DanyoEfecto efecto in dData.efectosDanyo)
            { // Sumamos el daño de todos los efectos
                danyoTotal += efecto.danyo;
                if (efecto.turnosRestantes > 0)
                {
                    efecto.turnosRestantes--;
                    if (efecto.turnosRestantes == 0)
                        aEliminar.Add(efecto); // Añadimos el efecto a la lista de efectos a eliminar si sus turnos llegan a 0
                }
            }

            // Aplicar daño de los efectos de este turno
            if (danyoTotal > 0)
            {
                int nuevaVida = dData.vida - danyoTotal;
                Debug.Log($"[Card] Efectos aplican {danyoTotal} de daño a {name}. Vida restante: {nuevaVida}");
                
                if (nuevaVida <= 0)
                { // La carta muere si recibe demasiado daño
                    casilla.LiberarCasilla(false);
                    return;
                }
                UpdateVida(nuevaVida);
            }

            // Limpiar efectos que acaban este turno
            foreach (DanyoEfecto efecto in aEliminar)
                dData.efectosDanyo.Remove(efecto);
        }
    }

    // Actualiza los turnos restantes de las cartas de tipo Trampa
    public void UpdateTurnos()
    {
        TrapCardData data = cardData as TrapCardData;
        data.turnos--;
        // textoVida en este caso indica los turnos restantes para que la trampa se rompa
        textoVida.text = data.turnos.ToString();
        background.SetActive(true);
        if (data.turnos == 0) // Si turnos llega a 0, se desturye la trampa
            casilla.LiberarCasilla(false);
    }

    // Restaura toda la durabilidad (turnos) de la trampa
    public void RestaurarTurnosTrampa()
    {
        if (cardData is TrapCardData data)
        {
            data.turnos = data.turnosMaximos;
            textoVida.text = data.turnos.ToString();
        }
    }

}
