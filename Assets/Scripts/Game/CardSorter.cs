using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class CardSorter : MonoBehaviour
{

    // Diccionario que asigna un peso a cada tipo (para ordenar)
    private Dictionary<CardType, int> ordenTipos = new Dictionary<CardType, int>
    {
        { CardType.Monstruo, 0 },
        { CardType.Estructura, 1 },
        { CardType.Hechizo, 2 },
        { CardType.Trampa, 3 },
        { CardType.MonstruoLeg, 4 },
        { CardType.Energia, 5 }
    };

    // Ordena las cartas de la mano del jugador
    public void Ordenar()
    {
        var cartas = GetComponentsInChildren<CardUI>()
                     .OrderBy(c => ordenTipos[c.cartaPrefab.cardData.tipo]) // Orden por tipo
                     .ThenBy(c => c.cartaPrefab.cardData.costoEnergia) // Dentro de cada tipo, ordenar por coste
                     .ThenBy(c => c.cartaPrefab.cardData.nombre) // Dentro de cada coste, ordenar por nombre
                     .ToList();

        for (int i = 0; i < cartas.Count; i++)
            cartas[i].transform.SetSiblingIndex(i);
        Resaltar();
    }

    // Resalta las cartas que se pueden usar de la mano del jugador
    public void Resaltar()
    {
        // Se ponen en gris las cartas que no tengan energía suficiente para ser usadas
        if (TurnManager.numTurno > 2)
        {
            var cartas = GetComponentsInChildren<CardUI>();
            foreach (var carta in cartas)
            {
                CardData data = carta.cartaPrefab.cardData;
                if (data.tipo != CardType.Energia && data.costoEnergia > TurnManager.energiaDisponible && carta.imagenUI.sprite != carta.spriteReverso)
                    carta.imagenUI.color = Color.gray;
                else // Si la carta se puede usar o está girada, se resalta
                    carta.imagenUI.color = Color.white;
            }
        }
    }
}
