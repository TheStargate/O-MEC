using System;
using UnityEngine;
using Unity.VisualScripting;
using System.Collections.Generic;
using System.Linq;

public class DeckManager : MonoBehaviour
{
    public CardUI cartaPrefab; // Prefab para instanciar cartas
    public Transform handPanelP1; // Mano del jugador 1
    public Transform handPanelP2; // Mano del jugador 2
    private CardData[] deckList; // Lista de cartas del juego (excepto energías)
    private CardData[] energyDeckList; // Lista de cartas de energía del juego
    public Queue<CardData> descartadasP1 = new Queue<CardData>(); // Lista de cartas descartadas del jugador 1
    public Queue<CardData> energiasDescartadasP1 = new Queue<CardData>(); // Lista de cartas de energía descartadas del jugador 1
    public Queue<CardData> descartadasP2 = new Queue<CardData>(); // Lista de cartas descartadas del jugador 2
    public Queue<CardData> energiasDescartadasP2 = new Queue<CardData>(); // Lista de cartas de energía descartadas del jugador 2
    public Queue<CardData> deckP1 = new Queue<CardData>(); // Lista de cartas en la baraja principal del jugador 1
    public Queue<CardData> energyDeckP1 = new Queue<CardData>(); // Lista de cartas en la baraja de energía del jugador 1
    public Queue<CardData> deckP2 = new Queue<CardData>(); // Lista de cartas en la baraja principal del jugador 2
    public Queue<CardData> energyDeckP2 = new Queue<CardData>(); // Lista de cartas en la baraja de energía del jugador 2

    // Devuelve los datos originales de una carta a partir de su nombre (para cargar partida)
    public CardData GetCardDataByName(string name)
    {
        if (deckList != null)
        {
            foreach (var card in deckList)
                if (card.nombre == name) return card;
        }
        if (energyDeckList != null)
        {
            foreach (var card in energyDeckList)
                if (card.nombre == name) return card;
        }
        return null;
    }

    // Modelos de las barajas en el juego (solo se muestran si hay cartas disponibles para robar)
    [SerializeField] private GameObject DeckP1;
    [SerializeField] private GameObject DeckP2;
    [SerializeField] private GameObject EnergyDeckP1;
    [SerializeField] private GameObject EnergyDeckP2;
    public static DeckManager Instance { get; private set; } // Instancia del propio objeto para comunicarse con otros scripts

    void Start()
    {
        Instance = this;

        // INSTANCIAR TODAS LAS CARTAS DEL JUEGO

        deckList = new CardData[]
        {
            new MonsterCardData
            {
                nombre = "Esqueleto",
                tipo = CardType.Monstruo,
                costoEnergia = 1,
                vida = 5,
                vidaMaxima = 5,
                ataque = 2,
                velocidad = 5,
                alcance = 1,
                costeHabilidad = 3
            },
            new MonsterCardData
            {
                nombre = "Espía",
                tipo = CardType.Monstruo,
                costoEnergia = 1,
                vida = 2,
                vidaMaxima = 2,
                ataque = 3,
                velocidad = 7,
                alcance = 1,
                costeHabilidad = 4
            },
            new MonsterCardData
            {
                nombre = "Esqueleto quemado",
                tipo = CardType.Monstruo,
                costoEnergia = 2,
                vida = 4,
                vidaMaxima = 4,
                ataque = 5,
                velocidad = 6,
                alcance = 1,
                costeHabilidad = 3
            },
            new MonsterCardData
            {
                nombre = "Guerrero",
                tipo = CardType.Monstruo,
                costoEnergia = 3,
                vida = 7,
                vidaMaxima = 7,
                ataque = 5,
                velocidad = 3,
                alcance = 1,
                costeHabilidad = 3
            },
            new MonsterCardData
            {
                nombre = "Arquero",
                tipo = CardType.Monstruo,
                costoEnergia = 4,
                vida = 3,
                vidaMaxima = 3,
                ataque = 4,
                velocidad = 6,
                alcance = 4,
                costeHabilidad = 6
            },
            new MonsterCardData
            {
                nombre = "Cura",
                tipo = CardType.Monstruo,
                costoEnergia = 4,
                vida = 8,
                vidaMaxima = 8,
                ataque = 5,
                velocidad = 3,
                alcance = 3,
                costeHabilidad = 7
            },
            new MonsterCardData
            {
                nombre = "Arquero reforzado",
                tipo = CardType.Monstruo,
                costoEnergia = 5,
                vida = 6,
                vidaMaxima = 6,
                ataque = 4,
                velocidad = 4,
                alcance = 3,
                costeHabilidad = 3
            },
            new MonsterCardData
            {
                nombre = "Caballero",
                tipo = CardType.Monstruo,
                costoEnergia = 5,
                vida = 9,
                vidaMaxima = 9,
                ataque = 3,
                velocidad = 5,
                alcance = 1,
                costeHabilidad = 4
            },
            new MonsterCardData
            {
                nombre = "Tanque",
                tipo = CardType.Monstruo,
                costoEnergia = 5,
                vida = 15,
                vidaMaxima = 15,
                ataque = 6,
                velocidad = 2,
                alcance = 3,
                costeHabilidad = 8
            },
            new MonsterCardData
            {
                nombre = "Bebé dragón",
                tipo = CardType.Monstruo,
                costoEnergia = 6,
                vida = 4,
                vidaMaxima = 4,
                ataque = 6,
                velocidad = 3,
                alcance = 3,
                costeHabilidad = 3
            },
            new MonsterCardData
            {
                nombre = "Arquero largo",
                tipo = CardType.Monstruo,
                costoEnergia = 6,
                vida = 4,
                vidaMaxima = 4,
                ataque = 6,
                velocidad = 5,
                alcance = 5,
                costeHabilidad = 3
            },
            new MonsterCardData
            {
                nombre = "Cura protector",
                tipo = CardType.Monstruo,
                costoEnergia = 6,
                vida = 6,
                vidaMaxima = 6,
                ataque = 7,
                velocidad = 3,
                alcance = 3,
                costeHabilidad = 4
            },
            new MonsterCardData
            {
                nombre = "Ninja",
                tipo = CardType.Monstruo,
                costoEnergia = 7,
                vida = 2,
                vidaMaxima = 2,
                ataque = 8,
                velocidad = 7,
                alcance = 2,
                costeHabilidad = 4
            },
            new MonsterCardData
            {
                nombre = "Dragón de fuego",
                tipo = CardType.Monstruo,
                costoEnergia = 8,
                vida = 8,
                vidaMaxima = 8,
                ataque = 10,
                velocidad = 4,
                alcance = 5,
                costeHabilidad = 5
            },
            new MonsterCardData
            {
                nombre = "Dragón de agua",
                tipo = CardType.Monstruo,
                costoEnergia = 8,
                vida = 11,
                vidaMaxima = 11,
                ataque = 7,
                velocidad = 4,
                alcance = 4,
                costeHabilidad = 5
            },
            new MonsterCardData
            {
                nombre = "Cura oscuro",
                tipo = CardType.Monstruo,
                costoEnergia = 8,
                vida = 13,
                vidaMaxima = 13,
                ataque = 6,
                velocidad = 4,
                alcance = 4,
                costeHabilidad = 5
            },
            new MonsterCardData
            {
                nombre = "Esqueleto gigante",
                tipo = CardType.Monstruo,
                costoEnergia = 9,
                vida = 14,
                vidaMaxima = 14,
                ataque = 4,
                velocidad = 2,
                alcance = 1,
                costeHabilidad = 6
            },
            new MonsterCardData
            {
                nombre = "Mago",
                tipo = CardType.Monstruo,
                costoEnergia = 9,
                vida = 5,
                vidaMaxima = 5,
                ataque = 5,
                velocidad = 4,
                alcance = 4,
                costeHabilidad = 7
            },
            new MonsterCardData
            {
                nombre = "Guerrero acorazado",
                tipo = CardType.Monstruo,
                costoEnergia = 10,
                vida = 16,
                vidaMaxima = 16,
                ataque = 3,
                velocidad = 1,
                alcance = 1,
                costeHabilidad = 4
            },
            new MonsterCardData
            {
                nombre = "Guerrero oscuro",
                tipo = CardType.Monstruo,
                costoEnergia = 11,
                vida = 10,
                vidaMaxima = 10,
                ataque = 5,
                velocidad = 3,
                alcance = 1,
                costeHabilidad = 6
            },
            new StructureCardData
            {
                nombre = "Muro",
                tipo = CardType.Estructura,
                costoEnergia = 2,
                vida = 60,
                vidaMaxima = 60,
                ataque = 0,
                alcance = 0,
                costeHabilidad = 1
            },
            new StructureCardData
            {
                nombre = "Torreta",
                tipo = CardType.Estructura,
                costoEnergia = 2,
                vida = 20,
                vidaMaxima = 20,
                ataque = 2,
                alcance = 3,
                costeHabilidad = 3
            },
            new StructureCardData
            {
                nombre = "Castillo falso",
                tipo = CardType.Estructura,
                costoEnergia = 2,
                vida = 30,
                vidaMaxima = 30,
                ataque = 0,
                alcance = 0,
                costeHabilidad = 5
            },
            new StructureCardData
            {
                nombre = "Herrería",
                tipo = CardType.Estructura,
                costoEnergia = 3,
                vida = 35,
                vidaMaxima = 35,
                ataque = 0,
                alcance = 0,
                costeHabilidad = 5
            },
            new StructureCardData
            {
                nombre = "Casa de constructor",
                tipo = CardType.Estructura,
                costoEnergia = 4,
                vida = 45,
                vidaMaxima = 45,
                ataque = 0,
                alcance = 0,
                costeHabilidad = 6
            },
            new StructureCardData
            {
                nombre = "Torre mágica",
                tipo = CardType.Estructura,
                costoEnergia = 5,
                vida = 40,
                vidaMaxima = 40,
                ataque = 2,
                alcance = 4,
                costeHabilidad = 2
            },
            new StructureCardData
            {
                nombre = "Muro reforzado",
                tipo = CardType.Estructura,
                costoEnergia = 5,
                vida = 80,
                vidaMaxima = 80,
                ataque = 0,
                alcance = 0,
                costeHabilidad = 3
            },
            new StructureCardData
            {
                nombre = "Torreta destructora",
                tipo = CardType.Estructura,
                costoEnergia = 6,
                vida = 25,
                vidaMaxima = 25,
                ataque = 7,
                alcance = 3,
                costeHabilidad = 4
            },
            new StructureCardData
            {
                nombre = "Torre protectora",
                tipo = CardType.Estructura,
                costoEnergia = 8,
                vida = 50,
                vidaMaxima = 50,
                ataque = 2,
                alcance = 5,
                costeHabilidad = 7
            },
            new StructureCardData
            {
                nombre = "Torre infernal",
                tipo = CardType.Estructura,
                costoEnergia = 10,
                vida = 15,
                vidaMaxima = 15,
                ataque = 1,
                alcance = 3,
                costeHabilidad = 6
            },
            new SpellCardData
            {
                nombre = "Lentitud eterna",
                tipo = CardType.Hechizo,
                costoEnergia = 4,
                requiereMonstruo = true
            },
            new SpellCardData
            {
                nombre = "Virus",
                tipo = CardType.Hechizo,
                costoEnergia = 4,
                requiereMonstruo = true
            },
            new SpellCardData
            {
                nombre = "Muerte instantánea",
                tipo = CardType.Hechizo,
                costoEnergia = 7,
                requiereMonstruo = true
            },
            new SpellCardData
            {
                nombre = "Flecha ardiente",
                tipo = CardType.Hechizo,
                costoEnergia = 6,
                requiereMonstruo = true
            },
            new SpellCardData
            {
                nombre = "Caos",
                tipo = CardType.Hechizo,
                costoEnergia = 7
            },
            new SpellCardData
            {
                nombre = "Explosivo",
                tipo = CardType.Hechizo,
                costoEnergia = 9,
                actuaEnArea = true,
                radioArea = 2
            },
            new SpellCardData
            {
                nombre = "Bola de fuego",
                tipo = CardType.Hechizo,
                costoEnergia = 11,
                actuaEnArea = true,
                radioArea = 1
            },
            new SpellCardData
            {
                nombre = "Bomba nuclear",
                tipo = CardType.Hechizo,
                costoEnergia = 17
            },
            new TrapCardData
            {
                nombre = "Clavos",
                tipo = CardType.Trampa,
                costoEnergia = 2,
                costeHabilidad = 2,
                ataque = 5,
                turnos = 6,
                turnosMaximos = 6
            },
            new TrapCardData
            {
                nombre = "Trampa eléctrica",
                tipo = CardType.Trampa,
                costoEnergia = 2,
                costeHabilidad = 2,
                ataque = 3,
                turnos = 9,
                turnosMaximos = 9
            },
            new TrapCardData
            {
                nombre = "Bomba",
                tipo = CardType.Trampa,
                costoEnergia = 2,
                costeHabilidad = 2,
                ataque = 12,
                turnos = 2,
                turnosMaximos = 2
            },
            new TrapCardData
            {
                nombre = "Trampa ígnea",
                tipo = CardType.Trampa,
                costoEnergia = 3,
                costeHabilidad = 1,
                ataque = 7,
                turnos = 3,
                turnosMaximos = 3
            },
            new TrapCardData
            {
                nombre = "Bombas",
                tipo = CardType.Trampa,
                costoEnergia = 3,
                costeHabilidad = 2,
                ataque = 10,
                turnos = 5,
                turnosMaximos = 5
            },
            new TrapCardData
            {
                nombre = "Pinchos",
                tipo = CardType.Trampa,
                costoEnergia = 3,
                costeHabilidad = 2,
                ataque = 6,
                turnos = 8,
                turnosMaximos = 8
            },
            new MonsterCardData
            {
                nombre = "Rey esqueleto",
                tipo = CardType.MonstruoLeg,
                costoEnergia = 10,
                vida = 20,
                vidaMaxima = 20,
                ataque = 3,
                velocidad = 1,
                alcance = 1,
                costeHabilidad = 0
            },
            new MonsterCardData
            {
                nombre = "Rey arquero",
                tipo = CardType.MonstruoLeg,
                costoEnergia = 10,
                vida = 8,
                vidaMaxima = 8,
                ataque = 7,
                velocidad = 7,
                alcance = 8,
                costeHabilidad = 0
            },
            new MonsterCardData
            {
                nombre = "Rey dragón",
                tipo = CardType.MonstruoLeg,
                costoEnergia = 10,
                vida = 14,
                vidaMaxima = 14,
                ataque = 12,
                velocidad = 3,
                alcance = 5,
                costeHabilidad = 0
            },
            new MonsterCardData
            {
                nombre = "Rey cura",
                tipo = CardType.MonstruoLeg,
                costoEnergia = 10,
                vida = 19,
                vidaMaxima = 19,
                ataque = 9,
                velocidad = 4,
                alcance = 5,
                costeHabilidad = 0
            },
            new MonsterCardData
            {
                nombre = "Rey guerrero",
                tipo = CardType.MonstruoLeg,
                costoEnergia = 10,
                vida = 34,
                vidaMaxima = 34,
                ataque = 12,
                velocidad = 3,
                alcance = 1,
                costeHabilidad = 0
            },
            new StructureCardData
            {
                nombre = "Castillo",
                tipo = CardType.Estructura,
                costoEnergia = 2,
                vida = 100,
                vidaMaxima = 100,
                ataque = 0,
                alcance = 0,
                costeHabilidad = 3
            }
        };

        energyDeckList = new CardData[]
        {
            new CardData
            {
                nombre = "Energía básica",
                tipo = CardType.Energia,
                costoEnergia = 1
            },
            new CardData
            {
                nombre = "Energía normal",
                tipo = CardType.Energia,
                costoEnergia = 2
            },
            new CardData
            {
                nombre = "Energía avanzada",
                tipo = CardType.Energia,
                costoEnergia = 3
            },
            new CardData
            {
                nombre = "Energía suprema",
                tipo = CardType.Energia,
                costoEnergia = 5
            }
        };

        // Establecer imágenes para las cartas

        foreach (var card in deckList)
        {
            string ruta = "Sprites/Cards/" + card.nombre.ToLower().Replace(" ", "_");
            card.imagenCarta = Resources.Load<Sprite>(ruta);

            if (card.imagenCarta == null)
                Debug.LogWarning($"No se encontró imagen para: {card.nombre} en {ruta}");
        }

        foreach (var card in energyDeckList)
        {
            string ruta = "Sprites/Cards/" + card.nombre.ToLower().Replace(" ", "_");
            card.imagenCarta = Resources.Load<Sprite>(ruta);

            if (card.imagenCarta == null)
                Debug.LogWarning($"No se encontró imagen para: {card.nombre} en {ruta}");
        }

        // Se genera un castillo (y 3 muros al colocarlo) para cada jugador al inicio de la partida
        CardUI nuevaCarta = Instantiate(cartaPrefab, handPanelP1);
        nuevaCarta.Setup(deckList[deckList.Length - 1]);
        nuevaCarta = Instantiate(cartaPrefab, handPanelP2);
        nuevaCarta.Setup(deckList[deckList.Length - 1]);

        InicializarMazo();
        InicializarMazoE();
    }

    // Si se puede, roba cartas de los mazos del jugador actual y las instancia
    public void SpawnCard()
    {

        // Comprueba si se puede robar en este turno
        if (TurnManager.robadoDisponible)
        {
            // Comprueba que la baraja del jugador actual tenga cartas disponibles para robar
            CardUI nuevaCarta;
            if (TurnManager.turnoP1)
            {
                if (deckP1.Count > 0)
                {
                    nuevaCarta = Instantiate(cartaPrefab, handPanelP1);
                    nuevaCarta.Setup(deckP1.Dequeue());
                }
                if (energyDeckP1.Count > 0)
                {
                    nuevaCarta = Instantiate(cartaPrefab, handPanelP1);
                    nuevaCarta.Setup(energyDeckP1.Dequeue());
                }
            }
            else
            {
                if (deckP2.Count > 0)
                {
                    nuevaCarta = Instantiate(cartaPrefab, handPanelP2);
                    nuevaCarta.Setup(deckP2.Dequeue());
                }
                if (energyDeckP2.Count > 0)
                {
                    nuevaCarta = Instantiate(cartaPrefab, handPanelP2);
                    nuevaCarta.Setup(energyDeckP2.Dequeue());
                }
            }

            recolocarBarajas(false);
            TurnManager.robadoDisponible = false;
        }
    }

    // Genera 3 cartas de muro cuando un jugador coloca su castillo
    public void SpawnWalls()
    {
        CardUI nuevaCarta;

        if (TurnManager.turnoP1)
        {
            nuevaCarta = Instantiate(cartaPrefab, handPanelP1);
            nuevaCarta.Setup(deckList[20]);
            nuevaCarta = Instantiate(cartaPrefab, handPanelP1);
            nuevaCarta.Setup(deckList[20]);
            nuevaCarta = Instantiate(cartaPrefab, handPanelP1);
            nuevaCarta.Setup(deckList[20]);
        }
        else
        {
            nuevaCarta = Instantiate(cartaPrefab, handPanelP2);
            nuevaCarta.Setup(deckList[20]);
            nuevaCarta = Instantiate(cartaPrefab, handPanelP2);
            nuevaCarta.Setup(deckList[20]);
            nuevaCarta = Instantiate(cartaPrefab, handPanelP2);
            nuevaCarta.Setup(deckList[20]);
        }
    }

    private void InicializarMazo()
    { // Establece las cartas del mazo principal de los jugadores
        deckP1.Clear();
        deckP1.Enqueue(deckList[0]);
        deckP1.Enqueue(deckList[0]);
        deckP1.Enqueue(deckList[1]);
        deckP1.Enqueue(deckList[1]);
        deckP1.Enqueue(deckList[2]);
        deckP1.Enqueue(deckList[3]);
        deckP1.Enqueue(deckList[3]);
        deckP1.Enqueue(deckList[4]);
        deckP1.Enqueue(deckList[5]);
        deckP1.Enqueue(deckList[6]);
        deckP1.Enqueue(deckList[7]);
        deckP1.Enqueue(deckList[8]);
        deckP1.Enqueue(deckList[9]);
        deckP1.Enqueue(deckList[10]);
        deckP1.Enqueue(deckList[11]);
        deckP1.Enqueue(deckList[12]);
        deckP1.Enqueue(deckList[13]);
        deckP1.Enqueue(deckList[14]);
        deckP1.Enqueue(deckList[15]);
        deckP1.Enqueue(deckList[16]);
        deckP1.Enqueue(deckList[17]);
        deckP1.Enqueue(deckList[17]);
        deckP1.Enqueue(deckList[18]);
        deckP1.Enqueue(deckList[19]);
        deckP1.Enqueue(deckList[20]);
        deckP1.Enqueue(deckList[20]);
        deckP1.Enqueue(deckList[21]);
        deckP1.Enqueue(deckList[21]);
        deckP1.Enqueue(deckList[22]);
        deckP1.Enqueue(deckList[23]);
        deckP1.Enqueue(deckList[24]);
        deckP1.Enqueue(deckList[25]);
        deckP1.Enqueue(deckList[26]);
        deckP1.Enqueue(deckList[27]);
        deckP1.Enqueue(deckList[28]);
        deckP1.Enqueue(deckList[29]);
        deckP1.Enqueue(deckList[30]);
        deckP1.Enqueue(deckList[30]);
        deckP1.Enqueue(deckList[31]);
        deckP1.Enqueue(deckList[31]);
        deckP1.Enqueue(deckList[32]);
        deckP1.Enqueue(deckList[33]);
        deckP1.Enqueue(deckList[34]);
        deckP1.Enqueue(deckList[35]);
        deckP1.Enqueue(deckList[36]);
        deckP1.Enqueue(deckList[37]);
        deckP1.Enqueue(deckList[38]);
        deckP1.Enqueue(deckList[39]);
        deckP1.Enqueue(deckList[40]);
        deckP1.Enqueue(deckList[41]);
        deckP1.Enqueue(deckList[42]);
        deckP1.Enqueue(deckList[43]);
        deckP1.Enqueue(deckList[44]);
        deckP1.Enqueue(deckList[45]);
        deckP1.Enqueue(deckList[46]);
        deckP1.Enqueue(deckList[47]);
        deckP1.Enqueue(deckList[48]);

        deckP2 = deckP1;
        BarajarMazo(true);
        BarajarMazo(false);
    }

    private void InicializarMazoE()
    { // Establece las cartas del mazo de energía de los jugadores
        energyDeckP1.Clear();

        // Añadir 7 básicas
        for (int i = 0; i < 7; i++)
            energyDeckP1.Enqueue(energyDeckList[0]);

        // Añadir 5 normales
        for (int i = 0; i < 5; i++)
            energyDeckP1.Enqueue(energyDeckList[1]);

        // Añadir 3 avanzadas
        for (int i = 0; i < 3; i++)
            energyDeckP1.Enqueue(energyDeckList[2]);

        // Añadir 2 supremas
        for (int i = 0; i < 2; i++)
            energyDeckP1.Enqueue(energyDeckList[3]);

        energyDeckP2 = energyDeckP1;
        BarajarMazoE(true);
        BarajarMazoE(false);
    }

    private void BarajarMazo(bool P1)
    {
        // Usar LINQ para barajar y meter en una cola
        if (P1)
            deckP1 = new Queue<CardData>(deckP1.OrderBy(c => UnityEngine.Random.value));
        else
            deckP2 = new Queue<CardData>(deckP2.OrderBy(c => UnityEngine.Random.value));
    }

    private void BarajarMazoE(bool P1)
    {
        // Usar LINQ para barajar y meter en una cola
        if (P1)
            energyDeckP1 = new Queue<CardData>(energyDeckP1.OrderBy(c => UnityEngine.Random.value));
        else
            energyDeckP2 = new Queue<CardData>(energyDeckP2.OrderBy(c => UnityEngine.Random.value));
    }

    // Pone en la pila de descartes la carta indicada del jugador indicado
    public void descartar(CardData data, bool P1)
    {
        if (P1)
            descartadasP1.Enqueue(data);
        else
            descartadasP2.Enqueue(data);
    }

    // Descarta la energía seleccionada    
    public void descartarEnergia()
    {
        CardData data = CardUI.cartaUISeleccionada.cartaPrefab.cardData;
        if (TurnManager.turnoP1)
            energiasDescartadasP1.Enqueue(data);
        else
            energiasDescartadasP2.Enqueue(data);
    }

    // Oculta las barajas que no tengan cartas y recoloca las cartas descartadas si no quedan más cartas para robar
    public void recolocarBarajas(bool cambioTurno)
    {
        // Se esconde la baraja si ya no quedan más cartas. Habrá que esperar un turno para recolocar las descartadas
        if (TurnManager.turnoP1)
        {
            // Si se acabó el mazo y se ha cambiado el turno, se vuelve a inicializar y barajar
            if (deckP1.Count == 0 && descartadasP1.Count > 0 && cambioTurno)
            {
                deckP1 = new Queue<CardData>(descartadasP1);
                descartadasP1.Clear();
                BarajarMazo(true);
            }
            if (energyDeckP1.Count == 0 && energiasDescartadasP1.Count > 0 && cambioTurno)
            {
                energyDeckP1 = new Queue<CardData>(energiasDescartadasP1);
                energiasDescartadasP1.Clear();
                BarajarMazoE(true);
            }

            // Se ocultan los mazos sin cartas
            if (deckP1.Count == 0)
                DeckP1.SetActive(false);
            else
                DeckP1.SetActive(true);
            if (energyDeckP1.Count == 0)
                EnergyDeckP1.SetActive(false);
            else
                EnergyDeckP1.SetActive(true);
        }
        else
        {
            // Si se acabó el mazo y se ha cambiado el turno, se vuelve a inicializar y barajar
            if (deckP2.Count == 0 && descartadasP2.Count > 0 && cambioTurno)
            {
                deckP2 = new Queue<CardData>(descartadasP2);
                descartadasP2.Clear();
                BarajarMazo(false);
            }
            if (energyDeckP2.Count == 0 && energiasDescartadasP2.Count > 0 && cambioTurno)
            {
                energyDeckP2 = new Queue<CardData>(energiasDescartadasP2);
                energiasDescartadasP2.Clear();
                BarajarMazoE(false);
            }

            // Se ocultan los mazos sin cartas
            if (deckP2.Count == 0)
                DeckP2.SetActive(false);
            else
                DeckP2.SetActive(true);
            if (energyDeckP2.Count == 0)
                EnergyDeckP2.SetActive(false);
            else
                EnergyDeckP2.SetActive(true);
        }
    }
}
