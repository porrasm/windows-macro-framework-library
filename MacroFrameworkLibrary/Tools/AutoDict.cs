﻿using System.Collections.Generic;

namespace MacroFramework.Tools {
    public class AutoDict<K, V> {
        public Dictionary<K, V> Dictionary { get; private set; }

        public AutoDict() {
            Dictionary = new Dictionary<K, V>();
        }

        public V this[K k] {
            get => Dictionary.ContainsKey(k) ? Dictionary[k] : default;
            set {
                if (Dictionary.ContainsKey(k)) {
                    Dictionary[k] = value;
                } else {
                    Dictionary.Add(k, value);
                }
            }
        }
    }
}
