using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace EscapeRoomFramework {

    internal class OrderedDictWrapper<TKey, TValue> {

        private OrderedDictionary backing = new OrderedDictionary();

        public int Count => backing.Count;

        public IEnumerable<TValue> Values {
            get {
                foreach (object value in backing.Values)
                    yield return (TValue) value;
            }
        }

        public TValue this[TKey key] {
            get { return (TValue) backing[(object) key]; }
            set { backing[(object) key] = value; }
        }

        public bool ContainsKey(TKey key) {
            return backing.Contains(key);
        }
    }

}