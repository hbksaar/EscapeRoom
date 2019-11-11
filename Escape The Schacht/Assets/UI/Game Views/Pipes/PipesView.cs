using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EscapeTheSchacht;
using EscapeTheSchacht.Pipes;
using UnityEngine.UI;
using System;
using EscapeTheSchacht.Trigger;
using EscapeRoomFramework;

namespace EscapeTheSchacht.UI {

    public class PipesView : MonoBehaviour {

        private PipesGame game;

        public GameObject gridElementPrefab;
        public Sprite fanSprite, valveSprite;

        private static readonly string fanCountStrF = "{0} / {1}";
        private static readonly Color activeFan = Color.green;
        private static readonly Color inactiveFan = Color.red;
        private static readonly Color openValve = new Color(.75f, 1f, .75f);
        private static readonly Color closedValve = new Color(1f, .75f, .75f);

        private Transform pnlGrid;
        private Dictionary<Vector2, Image> gridElementImages = new Dictionary<Vector2, Image>(); // Vector2: Vertex.pxCoords
        private Text lblFanCount;

        // Use this for initialization
        void Awake() {
            pnlGrid = transform.Find("pnlGrid");
            lblFanCount = transform.Find("lblFanCount").GetComponent<Text>();

            game = Ets.Room.GetGame<PipesGame>();
            game.OnGameStateChanged += OnGameStateChanged;
            game.OnValveTurned += OnValveTurned;
            game.OnFanTriggered += OnFanTriggered;
            //OnGameStateChanged(game, game.State, game.State);
        }

        public void OnGameStateChanged(Game<IEtsInterface> sender, GameStateChangedEventArgs e) {
            if (e.NewState == GameState.Initialized) {
                foreach (Image img in gridElementImages.Values)
                    Destroy(img.gameObject);
                gridElementImages.Clear();

                foreach (Fan fan in game.Fans()) {
                    GameObject go = Instantiate(gridElementPrefab, pnlGrid);
                    go.name = string.Format("Fan (row={0}, pos={1}, px={2})", fan.Row , fan.PositionInRow, fan.PxCoords);
                    go.layer = LayerMask.NameToLayer("UI");

                    GridElement gridElement = go.GetComponent<GridElement>();
                    gridElement.vertexRow = fan.Row;
                    gridElement.vertexPos = fan.PositionInRow;
                    gridElement.isValve = false;

                    Image img = go.GetComponent<Image>();
                    img.sprite = fanSprite;
                    img.color = fan.IsRunning ? activeFan : inactiveFan;

                    RectTransform rect = go.GetComponent<RectTransform>();
                    rect.sizeDelta = img.sprite.rect.size;
                    rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
                    Vector2 offset = new Vector2(-img.sprite.rect.size.x / 2, img.sprite.rect.size.y / 2);
                    rect.anchoredPosition = fan.PxCoords + offset;

                    gridElementImages[fan.PxCoords] = img;
                }

                foreach (Valve valve in game.Valves()) {
                    GameObject go = Instantiate(gridElementPrefab, pnlGrid);
                    go.name = string.Format("Valve (row={0}, pos={1}, px={2})", valve.Row, valve.PositionInRow, valve.PxCoords);
                    go.layer = LayerMask.NameToLayer("UI");

                    GridElement gridElement = go.GetComponent<GridElement>();
                    gridElement.vertexRow = valve.Row;
                    gridElement.vertexPos = valve.PositionInRow;
                    gridElement.isValve = true;

                    Image img = go.GetComponent<Image>();
                    img.sprite = valveSprite;
                    img.color = valve.IsOpen ? openValve : closedValve;

                    RectTransform rect = go.GetComponent<RectTransform>();
                    rect.sizeDelta = img.sprite.rect.size;
                    rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
                    Vector2 offset = new Vector2(-img.sprite.rect.size.x / 2, img.sprite.rect.size.y / 2);
                    rect.anchoredPosition = valve.PxCoords + offset;

                    gridElementImages[valve.PxCoords] = img;
                }

                lblFanCount.text = string.Format(fanCountStrF, game.RunningFansCount, game.FanCount);
            }
        }

        public void OnValveTurned(PipesGame sender, ValveTurnedEventArgs e) {
            //print("PipesView.OnValveTurned: " + valve.X + " " + valve.Y);
            gridElementImages[e.Valve.PxCoords].color = e.Valve.IsOpen ? openValve : closedValve;
        }

        public void OnFanTriggered(PipesGame sender, FanTriggeredEventArgs e) {
            //print("PipesView.OnFanTriggered: " + fan.X + " " + fan.Y);
            gridElementImages[e.Fan.PxCoords].color = e.Fan.IsRunning ? activeFan : inactiveFan;
            lblFanCount.text = string.Format(fanCountStrF, sender.RunningFansCount, sender.FanCount);
        }

        public void OnClickGridElement(GridElement sender) {
            Log.Debug("Clicked grid element (valve: {0}, row={1}, pos={2})", sender.isValve, sender.vertexRow, sender.vertexPos);
            if (Ets.Room == null)
                return;
            if (game.State != GameState.Running)
                return;
            if (!sender.isValve)
                return;

            EtsMockupInterface mockup = Ets.Room.Physical as EtsMockupInterface;
            if (mockup != null)
                mockup.ToggleValve(sender.vertexRow, sender.vertexPos);
        }

    }

}