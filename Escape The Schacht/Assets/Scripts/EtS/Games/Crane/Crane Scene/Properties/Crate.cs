using UnityEngine;

namespace EscapeTheSchacht.Crates {

    public class Crate : MonoBehaviour {

        public int id;

        public bool isInDropzone;

        public Transform CrateHub { get; private set; }

        private void Start() {
            CrateHub = transform.parent;
        }

    }

}
