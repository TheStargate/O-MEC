using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

public class DamageableCardData : CardData
{
    public int vida; // Vida actual de la carta
    public int vidaMaxima;
    public int ataque;
    public int alcance;

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
        };
    }
}

[System.Serializable]
public class MonsterCardData : DamageableCardData
{
    public int velocidad;
    public int costeHabilidad;

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
    public int costeHabilidad;

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
    public int costeHabilidad;
    public int ataque;
    public int turnos;

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
            turnos = this.turnos
        };
    }
}

public class Card : MonoBehaviour
{
    public CardData cardData; // Información de la carta
    public Cell casilla; // Casilla en la que está colocada la carta
    public ClickableObject clickableObject; // Para establecer la carta como clickable
    [SerializeField] private TextMeshPro textoVida; // Indica la vida actual de la carta
    public GameObject background; // Fondo para que se vea bien textoVida
    
    // Instancia una nueva carta a partir de los datos indicados
    public void Setup(CardData data)
    {
        background.SetActive(false);
        cardData = data.Clone();
        name = data.nombre;
        if (data.tipo != CardType.Trampa) // Si la carta es un trampa, no se muestra su imagen
            clickableObject.renderizador.material.mainTexture = data.imagenCarta.texture;
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

    // Actualiza y muestra la nueva vida de la carta
    public void UpdateVida(int nuevaVida)
    {
        DamageableCardData data = cardData as DamageableCardData;
        data.vida = nuevaVida;
        textoVida.text = data.vida.ToString();
        if (data.vidaMaxima == nuevaVida)
            background.SetActive(false);
        else
            background.SetActive(true);
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

}
