using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EscapeTheSchacht.UI {

    public class GridElement : MonoBehaviour, IPointerClickHandler {

        public int vertexRow, vertexPos;
        public bool isValve;

        private PipesView view;

        // Use this for initialization
        void Awake() {
            view = transform.parent.GetComponentInParent<PipesView>();
        }

        private void OnMouseDown() {
            print("OnMouseDown");
            view.OnClickGridElement(this);
        }

        public void OnPointerClick(PointerEventData eventData) {
            print("OnPointerClick");
            view.OnClickGridElement(this);
        }

    }

}