/*
   Licensed to the Apache Software Foundation (ASF) under one or more
   contributor license agreements.  See the NOTICE file distributed with
   this work for additional information regarding copyright ownership.
   The ASF licenses this file to You under the Apache License, Version 2.0
   (the "License"); you may not use this file except in compliance with
   the License.  You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
 */
using java.lang;
using java.util;

namespace stab.query {

    public interface OrderedIterable<TSource> : Iterable<TSource> {
        OrderedIterable<TSource> createOrderedIterable<TKey>(FunctionTT<TSource, TKey> keySelector,
            Comparator<TKey> comparator, bool descending);
        OrderedIterable<TSource> createOrderedIterable(FunctionTInt<TSource> keySelector, bool descending);
        OrderedIterable<TSource> createOrderedIterable(FunctionTLong<TSource> keySelector, bool descending);
        OrderedIterable<TSource> createOrderedIterable(FunctionTFloat<TSource> keySelector, bool descending);
        OrderedIterable<TSource> createOrderedIterable(FunctionTDouble<TSource> keySelector, bool descending);
    }
    
    abstract class OrderedEnumerable<TSource> : OrderedIterable<TSource> {
        protected Iterable<TSource> source*;
        protected bool descending*;

        OrderedEnumerable(Iterable<TSource> source, bool descending) {
            if (source == null) {
                throw new NullPointerException("source");
            }
            this.source = source;
            this.descending = descending;
        }

        abstract Sorter getSorter(List<TSource> values, Sorter next);
        
        public OrderedIterable<TSource> createOrderedIterable<TKey>(FunctionTT<TSource, TKey> keySelector,
                Comparator<TKey> comparator, bool descending) {
            return new KeyOrderedIterable<TSource, TKey>(source, keySelector, comparator, descending, this);
        }
        
        public OrderedIterable<TSource> createOrderedIterable(FunctionTInt<TSource> keySelector, bool descending) {
            return new IntKeyOrderedIterable<TSource>(source, keySelector, descending, this);
        }
        
        public OrderedIterable<TSource> createOrderedIterable(FunctionTLong<TSource> keySelector, bool descending) {
            return new LongKeyOrderedIterable<TSource>(source, keySelector, descending, this);
        }
        
        public OrderedIterable<TSource> createOrderedIterable(FunctionTFloat<TSource> keySelector, bool descending) {
            return new FloatKeyOrderedIterable<TSource>(source, keySelector, descending, this);
        }
        
        public OrderedIterable<TSource> createOrderedIterable(FunctionTDouble<TSource> keySelector, bool descending) {
            return new DoubleKeyOrderedIterable<TSource>(source, keySelector, descending, this);
        }
        
        protected OrderedIterable<TSource> getOrderedEnumerable() {
            return this;
        }
    }
    
    class KeyOrderedIterable<TSource, TKey> : OrderedEnumerable<TSource> {
        private FunctionTT<TSource, TKey> keySelector;
        private Comparator<TKey> comparator;
        private OrderedEnumerable<TSource> parent;
        
        KeyOrderedIterable(Iterable<TSource> source, FunctionTT<TSource, TKey> keySelector, Comparator<TKey> comparator,
                bool descending, OrderedEnumerable<TSource> parent)
            : super(source, descending) {
            if (keySelector == null) {
                throw new NullPointerException("keySelector");
            }
            this.keySelector = keySelector;
            if (comparator == null) {
                #pragma warning disable 270 // Ignore warning about raw generic types
                comparator = (Comparator<TKey>)DefaultComparator.INSTANCE; // Assumes that TKey extends Comparable<TKey>
                #pragma warning restore
            }
            this.comparator = comparator;
            this.parent = parent;
        }

        public override Sorter getSorter(List<TSource> values, Sorter next) {
            Sorter result = new KeySorter<TSource, TKey>(values, keySelector, comparator, descending, next);
            if (parent != null) {
                result = parent.getSorter(values, result);
            }
            return result;
        }
        
        public Iterator<TSource> iterator() {
            var values = source.toList();
            int size = values.size();
            if (size == 0) {
                yield break;
            }
            var sorter = getSorter(values, null);
            var map = Sorter.createMap(size);
            sorter.sort(map, 0, size - 1);
            for (int i = 0; i < size; i++) {
                yield return values[map[i]];
            }
        }
        
        private class DefaultComparator<T> : Comparator<T>
                where T : Comparable<T>
        {
            #pragma warning disable 252 // Ignore warning about raw generic types
            static Comparator INSTANCE = new DefaultComparator();
            #pragma warning restore
        
            public int compare(T o1, T o2) {
                return o1.compareTo(o2);
            }
        }
        
        private class KeySorter<TSource, TKey> : Sorter {
            private Sorter next;
            private TKey[] keys;
            private Comparator<TKey> comparator;
            private bool descending;
            private List<TSource> values;
            private FunctionTT<TSource, TKey> keySelector;

            KeySorter(List<TSource> values, FunctionTT<TSource, TKey> keySelector,
                    Comparator<TKey> comparator, bool descending, Sorter next) {
                this.values = values;
                this.keySelector = keySelector;
                this.comparator = comparator;
                this.descending = descending;
                this.next = next;
            }
            
            protected override int compare(int index1, int index2) {
                if (keys == null) {
                    #pragma warning disable 313
                    keys = new TKey[values.size()];
                    #pragma warning restore
                    for (int i = 0; i < sizeof(keys); i++) {
                        keys[i] = keySelector.invoke(values[i]);
                    }
                }
                int comp = comparator.compare(keys[index1], keys[index2]);
                if (comp == 0) {
                    if (next == null) {
                        return index1 - index2;
                    } else {
                        return next.compare(index1, index2);
                    }
                }
                if (descending) {
                    return -comp;
                } else {
                    return comp;
                }
            }
        }
    }
    
    class IntKeyOrderedIterable<TSource> : OrderedEnumerable<TSource> {
        private FunctionTInt<TSource> keySelector;
        private OrderedEnumerable<TSource> parent;
        
        IntKeyOrderedIterable(Iterable<TSource> source, FunctionTInt<TSource> keySelector, bool descending, OrderedEnumerable<TSource> parent)
            : super(source, descending) {
            if (keySelector == null) {
                throw new NullPointerException("keySelector");
            }
            this.keySelector = keySelector;
            this.parent = parent;
        }

        public override Sorter getSorter(List<TSource> values, Sorter next) {
            Sorter result = new KeySorter<TSource>(values, keySelector, descending, next);
            if (parent != null) {
                result = parent.getSorter(values, result);
            }
            return result;
        }
        
        public Iterator<TSource> iterator() {
            var values = source.toList();
            int size = values.size();
            if (size == 0) {
                yield break;
            }
            var sorter = getSorter(values, null);
            var map = Sorter.createMap(size);
            sorter.sort(map, 0, size - 1);
            for (int i = 0; i < size; i++) {
                yield return values[map[i]];
            }
        }
        
        private class KeySorter<TSource> : Sorter {
            private Sorter next;
            private int[] keys;
            private bool descending;
            private List<TSource> values;
            private FunctionTInt<TSource> keySelector;

            KeySorter(List<TSource> values, FunctionTInt<TSource> keySelector, bool descending, Sorter next) {
                this.values = values;
                this.keySelector = keySelector;
                this.descending = descending;
                this.next = next;
            }
            
            protected override int compare(int index1, int index2) {
                if (keys == null) {
                    keys = new int[values.size()];
                    for (int i = 0; i < sizeof(keys); i++) {
                        keys[i] = keySelector.invoke(values[i]);
                    }
                }
                var k1 = keys[index1];
                var k2 = keys[index2];
                int comp = (k1 < k2) ? -1 : (k1 == k2) ? 0 : 1;
                if (comp == 0) {
                    if (next == null) {
                        return index1 - index2;
                    } else {
                        return next.compare(index1, index2);
                    }
                }
                return (descending) ? -comp : comp;
            }
        }
    }
    
    class LongKeyOrderedIterable<TSource> : OrderedEnumerable<TSource> {
        private FunctionTLong<TSource> keySelector;
        private OrderedEnumerable<TSource> parent;
        
        LongKeyOrderedIterable(Iterable<TSource> source, FunctionTLong<TSource> keySelector, bool descending, OrderedEnumerable<TSource> parent)
            : super(source, descending) {
            if (keySelector == null) {
                throw new NullPointerException("keySelector");
            }
            this.keySelector = keySelector;
            this.parent = parent;
        }

        public override Sorter getSorter(List<TSource> values, Sorter next) {
            Sorter result = new KeySorter<TSource>(values, keySelector, descending, next);
            if (parent != null) {
                result = parent.getSorter(values, result);
            }
            return result;
        }
        
        public Iterator<TSource> iterator() {
            var values = source.toList();
            int size = values.size();
            if (size == 0) {
                yield break;
            }
            var sorter = getSorter(values, null);
            var map = Sorter.createMap(size);
            sorter.sort(map, 0, size - 1);
            for (int i = 0; i < size; i++) {
                yield return values[map[i]];
            }
        }
        
        private class KeySorter<TSource> : Sorter {
            private Sorter next;
            private long[] keys;
            private bool descending;
            private List<TSource> values;
            private FunctionTLong<TSource> keySelector;

            KeySorter(List<TSource> values, FunctionTLong<TSource> keySelector, bool descending, Sorter next) {
                this.values = values;
                this.keySelector = keySelector;
                this.descending = descending;
                this.next = next;
            }
            
            protected override int compare(int index1, int index2) {
                if (keys == null) {
                    keys = new long[values.size()];
                    for (int i = 0; i < sizeof(keys); i++) {
                        keys[i] = keySelector.invoke(values[i]);
                    }
                }
                var k1 = keys[index1];
                var k2 = keys[index2];
                int comp = (k1 < k2) ? -1 : (k1 == k2) ? 0 : 1;
                if (comp == 0) {
                    if (next == null) {
                        return index1 - index2;
                    } else {
                        return next.compare(index1, index2);
                    }
                }
                return (descending) ? -comp : comp;
            }
        }
    }
    
    class FloatKeyOrderedIterable<TSource> : OrderedEnumerable<TSource> {
        private FunctionTFloat<TSource> keySelector;
        private OrderedEnumerable<TSource> parent;
        
        FloatKeyOrderedIterable(Iterable<TSource> source, FunctionTFloat<TSource> keySelector, bool descending,
                OrderedEnumerable<TSource> parent)
            : super(source, descending) {
            if (keySelector == null) {
                throw new NullPointerException("keySelector");
            }
            this.keySelector = keySelector;
            this.parent = parent;
        }

        public override Sorter getSorter(List<TSource> values, Sorter next) {
            Sorter result = new KeySorter<TSource>(values, keySelector, descending, next);
            if (parent != null) {
                result = parent.getSorter(values, result);
            }
            return result;
        }
        
        public Iterator<TSource> iterator() {
            var values = source.toList();
            int size = values.size();
            if (size == 0) {
                yield break;
            }
            var sorter = getSorter(values, null);
            var map = Sorter.createMap(size);
            sorter.sort(map, 0, size - 1);
            for (int i = 0; i < size; i++) {
                yield return values[map[i]];
            }
        }
        
        private class KeySorter<TSource> : Sorter {
            private Sorter next;
            private float[] keys;
            private bool descending;
            private List<TSource> values;
            private FunctionTFloat<TSource> keySelector;

            KeySorter(List<TSource> values, FunctionTFloat<TSource> keySelector, bool descending, Sorter next) {
                this.values = values;
                this.keySelector = keySelector;
                this.descending = descending;
                this.next = next;
            }
            
            protected override int compare(int index1, int index2) {
                if (keys == null) {
                    keys = new float[values.size()];
                    for (int i = 0; i < sizeof(keys); i++) {
                        keys[i] = keySelector.invoke(values[i]);
                    }
                }
                var k1 = keys[index1];
                var k2 = keys[index2];
                int comp = (k1 < k2) ? -1 : (k1 == k2) ? 0 : 1;
                if (comp == 0) {
                    if (next == null) {
                        return index1 - index2;
                    } else {
                        return next.compare(index1, index2);
                    }
                }
                return (descending) ? -comp : comp;
            }
        }
    }
    
    class DoubleKeyOrderedIterable<TSource> : OrderedEnumerable<TSource> {
        private FunctionTDouble<TSource> keySelector;
        private OrderedEnumerable<TSource> parent;
        
        DoubleKeyOrderedIterable(Iterable<TSource> source, FunctionTDouble<TSource> keySelector, bool descending,
                OrderedEnumerable<TSource> parent)
            : super(source, descending) {
            if (keySelector == null) {
                throw new NullPointerException("keySelector");
            }
            this.keySelector = keySelector;
            this.parent = parent;
        }

        public override Sorter getSorter(List<TSource> values, Sorter next) {
            Sorter result = new KeySorter<TSource>(values, keySelector, descending, next);
            if (parent != null) {
                result = parent.getSorter(values, result);
            }
            return result;
        }
        
        public Iterator<TSource> iterator() {
            var values = source.toList();
            int size = values.size();
            if (size == 0) {
                yield break;
            }
            var sorter = getSorter(values, null);
            var map = Sorter.createMap(size);
            sorter.sort(map, 0, size - 1);
            for (int i = 0; i < size; i++) {
                yield return values[map[i]];
            }
        }
        
        private class KeySorter<TSource> : Sorter {
            private Sorter next;
            private double[] keys;
            private bool descending;
            private List<TSource> values;
            private FunctionTDouble<TSource> keySelector;

            KeySorter(List<TSource> values, FunctionTDouble<TSource> keySelector, bool descending, Sorter next) {
                this.values = values;
                this.keySelector = keySelector;
                this.descending = descending;
                this.next = next;
            }
            
            protected override int compare(int index1, int index2) {
                if (keys == null) {
                    keys = new double[values.size()];
                    for (int i = 0; i < sizeof(keys); i++) {
                        keys[i] = keySelector.invoke(values[i]);
                    }
                }
                var k1 = keys[index1];
                var k2 = keys[index2];
                int comp = (k1 < k2) ? -1 : (k1 == k2) ? 0 : 1;
                if (comp == 0) {
                    if (next == null) {
                        return index1 - index2;
                    } else {
                        return next.compare(index1, index2);
                    }
                }
                return (descending) ? -comp : comp;
            }
        }
    }
}