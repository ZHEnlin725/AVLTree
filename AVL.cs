using System;

namespace AVL
{
    public abstract partial class AVLTreeBase<TValue>
    {
        protected partial class AVLTreeNode : AVLTree_API.IAVLTreeNode
        {
            public TValue value;

            public int height { get; set; }

            public int parent_index { get; set; } = AVLTree_API.None;

            public int left_child_index { get; set; } = AVLTree_API.None;

            public int right_child_index { get; set; } = AVLTree_API.None;
        }

        protected int size, rootIndex = AVLTree_API.None;

        protected Func<TValue, TValue, int> compareFunc;
        protected AVLTreeNode[] nodes;

        public int numNodes => size;

        public AVLTreeBase(Func<TValue, TValue, int> compareFunc, int capacity = 32)
        {
            this.compareFunc = compareFunc;
            this.nodes = new AVLTreeNode[Math.Max(32, capacity)];
        }

        public AVLTreeBase(int capacity = 32) : this((x, y) =>
            ((IComparable<TValue>) x).CompareTo(y), capacity)
        {
            if (!typeof(IComparable<TValue>).IsAssignableFrom(typeof(TValue)))
                throw new ArgumentException($"{typeof(IComparable)} Must Assignable From {typeof(TValue)}");
        }

        public virtual void Insert(TValue value)
        {
            if (value == null) return;
            rootIndex = insert(rootIndex, value);
        }

        public virtual bool Delete(TValue value)
        {
            if (value == null) return false;
            var index = rootIndex;
            if (tryDelete(ref index, value, out var deletedIndex))
            {
                rootIndex = index;
                AVLTree_API.fillup(deletedIndex, --size, nodes, ref rootIndex);
                return true;
            }

            return default;
        }

        public virtual void Clear()
        {
            AVLTree_API.destroy(rootIndex, nodes);
            rootIndex = AVLTree_API.None;
            size = 0;
        }

        protected virtual bool tryDelete(ref int nodeIndex, TValue value, out int deletedIndex)
        {
            deletedIndex = AVLTree_API.None;
            if (nodeIndex < 0 || nodeIndex >= size)
                return false;
            var node = nodes[nodeIndex];
            if (compareFunc(value, node.value) < 0)
            {
                var left_child_index = node.left_child_index;
                if (tryDelete(ref left_child_index, value, out deletedIndex))
                {
                    node.left_child_index = left_child_index;
                    AVLTree_API.udpate_height(nodeIndex, nodes);
                    nodeIndex = AVLTree_API.self_balancing(nodeIndex, nodes);
                    return true;
                }
            }
            else if (compareFunc(value, node.value) > 0)
            {
                var right_child_index = node.right_child_index;
                if (tryDelete(ref right_child_index, value, out deletedIndex))
                {
                    node.right_child_index = right_child_index;
                    AVLTree_API.udpate_height(nodeIndex, nodes);
                    nodeIndex = AVLTree_API.self_balancing(nodeIndex, nodes);
                    return true;
                }
            }
            else
            {
                var left_child_index = node.left_child_index;
                var right_child_index = node.right_child_index;
                var has_left = left_child_index != AVLTree_API.None;
                var has_right = right_child_index != AVLTree_API.None;
                if (has_left && has_right)
                {
                    var child_index = right_child_index;
                    while (child_index >= 0 && child_index < size &&
                           nodes[child_index].left_child_index != AVLTree_API.None)
                        child_index = nodes[child_index].left_child_index;

                    var child_node = nodes[child_index];
                    var child_val = child_node.value;

                    if (tryDelete(ref right_child_index, value, out deletedIndex))
                    {
                        node.value = child_val;
                        node.right_child_index = right_child_index;
                        AVLTree_API.udpate_height(nodeIndex, nodes);
                        nodeIndex = AVLTree_API.self_balancing(nodeIndex, nodes);
                        return true;
                    }
                }
                else
                {
                    var child_index = AVLTree_API.None;
                    if (has_left)
                    {
                        child_index = left_child_index;
                    }
                    else if (has_right)
                    {
                        child_index = right_child_index;
                    }

                    if (child_index != AVLTree_API.None)
                    {
                        var child_node = nodes[child_index];
                        var child_left_child_index = child_node.left_child_index;
                        var child_right_child_index = child_node.right_child_index;

                        AVLTree_API.set_parent(child_index, node.parent_index, nodes);

                        AVLTree_API.swap(nodes, child_index, nodeIndex);

                        AVLTree_API.set_parent(child_left_child_index, nodeIndex, nodes);
                        AVLTree_API.set_parent(child_right_child_index, nodeIndex, nodes);

                        deletedIndex = child_index;
                        nodes[deletedIndex] = null;
                    }
                    else
                    {
                        deletedIndex = nodeIndex;
                        nodes[deletedIndex] = null;
                        nodeIndex = AVLTree_API.None;
                    }

                    return true;
                }
            }

            return false;
        }

        protected virtual int insert(int nodeIndex, TValue value, int parentIndex = AVLTree_API.None)
        {
            if (nodeIndex == AVLTree_API.None) nodeIndex = size++;
            nodes = AVLTree_API.ensure_capacity(nodes, nodeIndex + 1);
            var node = nodes[nodeIndex];
            if (node == null)
            {
                var treeNode = createNode();
                treeNode.value = value;
                treeNode.parent_index = parentIndex;
                nodes[nodeIndex] = treeNode;
            }
            else if (compareFunc(value, node.value) < 0)
            {
                node.left_child_index = insert(node.left_child_index, value, nodeIndex);
            }
            else
            {
                node.right_child_index = insert(node.right_child_index, value, nodeIndex);
            }

            AVLTree_API.udpate_height(nodeIndex, nodes);
            return AVLTree_API.self_balancing(nodeIndex, nodes);
        }

        protected virtual AVLTreeNode createNode() => new AVLTreeNode();
    }

    public abstract partial class AVLTreeBase_KV<TKey, TValue>
    {
        protected partial class AVLTreeNode : AVLTree_API.IAVLTreeNode
        {
            public TKey key;
            public TValue value;

            public int height { get; set; }

            public int parent_index { get; set; } = AVLTree_API.None;

            public int left_child_index { get; set; } = AVLTree_API.None;

            public int right_child_index { get; set; } = AVLTree_API.None;
        }

        protected int size, rootIndex = AVLTree_API.None;

        protected Func<TKey, TKey, int> compareFunc;
        protected AVLTreeNode[] nodes;

        public int numNodes => size;

        public AVLTreeBase_KV(Func<TKey, TKey, int> compareFunc, int capacity = 32)
        {
            this.compareFunc = compareFunc;
            this.nodes = new AVLTreeNode[Math.Max(32, capacity)];
        }

        public AVLTreeBase_KV(int capacity = 32) : this((x, y) =>
            ((IComparable<TKey>) x).CompareTo(y), capacity)
        {
            if (!typeof(IComparable<TKey>).IsAssignableFrom(typeof(TKey)))
                throw new ArgumentException($"{typeof(IComparable)} Must Assignable From {typeof(TKey)}");
        }

        public virtual TValue Query(TKey key) =>
            key == null ? default : query(rootIndex, key);

        public virtual void Insert(TKey key, TValue value)
        {
            if (key == null) return;
            rootIndex = insert(rootIndex, key, value);
        }

        public virtual bool Delete(TKey key, out TValue value)
        {
            value = default;
            if (key == null) return false;
            var index = rootIndex;
            if (tryDelete(ref index, key, out value, out var deletedIndex))
            {
                rootIndex = index;
                AVLTree_API.fillup(deletedIndex, --size, nodes, ref rootIndex);
                return true;
            }

            return default;
        }

        public virtual void Clear()
        {
            AVLTree_API.destroy(rootIndex, nodes);
            rootIndex = AVLTree_API.None;
            size = 0;
        }

        protected virtual bool tryDelete(ref int nodeIndex, TKey key, out TValue value, out int deletedIndex)
        {
            value = default;
            deletedIndex = AVLTree_API.None;
            if (nodeIndex < 0 || nodeIndex >= size)
                return false;
            var node = nodes[nodeIndex];
            if (compareFunc(key, node.key) < 0)
            {
                var left_child_index = node.left_child_index;
                if (tryDelete(ref left_child_index, key, out value, out deletedIndex))
                {
                    node.left_child_index = left_child_index;
                    AVLTree_API.udpate_height(nodeIndex, nodes);
                    nodeIndex = AVLTree_API.self_balancing(nodeIndex, nodes);
                    return true;
                }
            }
            else if (compareFunc(key, node.key) > 0)
            {
                var right_child_index = node.right_child_index;
                if (tryDelete(ref right_child_index, key, out value, out deletedIndex))
                {
                    node.right_child_index = right_child_index;
                    AVLTree_API.udpate_height(nodeIndex, nodes);
                    nodeIndex = AVLTree_API.self_balancing(nodeIndex, nodes);
                    return true;
                }
            }
            else
            {
                var left_child_index = node.left_child_index;
                var right_child_index = node.right_child_index;
                var has_left = left_child_index != AVLTree_API.None;
                var has_right = right_child_index != AVLTree_API.None;
                if (has_left && has_right)
                {
                    var child_index = right_child_index;
                    while (child_index >= 0 && child_index < size &&
                           nodes[child_index].left_child_index != AVLTree_API.None)
                        child_index = nodes[child_index].left_child_index;

                    var child_node = nodes[child_index];
                    var child_key = child_node.key;
                    var child_val = child_node.value;

                    if (tryDelete(ref right_child_index, child_key, out value, out deletedIndex))
                    {
                        node.key = child_key;
                        node.value = child_val;
                        node.right_child_index = right_child_index;
                        AVLTree_API.udpate_height(nodeIndex, nodes);
                        nodeIndex = AVLTree_API.self_balancing(nodeIndex, nodes);
                        return true;
                    }
                }
                else
                {
                    var child_index = AVLTree_API.None;
                    if (has_left)
                    {
                        child_index = left_child_index;
                    }
                    else if (has_right)
                    {
                        child_index = right_child_index;
                    }

                    if (child_index != AVLTree_API.None)
                    {
                        var child_node = nodes[child_index];
                        var child_left_child_index = child_node.left_child_index;
                        var child_right_child_index = child_node.right_child_index;

                        AVLTree_API.set_parent(child_index, node.parent_index, nodes);

                        AVLTree_API.swap(nodes, child_index, nodeIndex);

                        AVLTree_API.set_parent(child_left_child_index, nodeIndex, nodes);
                        AVLTree_API.set_parent(child_right_child_index, nodeIndex, nodes);

                        deletedIndex = child_index;
                        value = nodes[deletedIndex].value;
                        nodes[deletedIndex] = null;
                    }
                    else
                    {
                        deletedIndex = nodeIndex;
                        value = nodes[deletedIndex].value;
                        nodes[deletedIndex] = null;
                        nodeIndex = AVLTree_API.None;
                    }

                    return true;
                }
            }

            return false;
        }

        protected virtual TValue query(int nodeIndex, TKey key)
        {
            if (nodeIndex < 0 || nodeIndex >= size)
                return default;
            var node = nodes[nodeIndex];
            var compareVal = compareFunc(key, node.key);
            return compareVal < 0
                ? query(node.left_child_index, key)
                : compareVal > 0
                    ? query(node.right_child_index, key)
                    : node.value;
        }

        protected virtual int insert(int nodeIndex, TKey key, TValue value, int parentIndex = AVLTree_API.None)
        {
            if (nodeIndex == AVLTree_API.None) nodeIndex = size++;
            nodes = AVLTree_API.ensure_capacity(nodes, nodeIndex + 1);
            var node = nodes[nodeIndex];
            if (node == null)
            {
                var treeNode = createNode();
                treeNode.key = key;
                treeNode.value = value;
                treeNode.parent_index = parentIndex;
                nodes[nodeIndex] = treeNode;
            }
            else if (compareFunc(key, node.key) < 0)
            {
                node.left_child_index = insert(node.left_child_index, key, value, nodeIndex);
            }
            else
            {
                node.right_child_index = insert(node.right_child_index, key, value, nodeIndex);
            }

            AVLTree_API.udpate_height(nodeIndex, nodes);
            return AVLTree_API.self_balancing(nodeIndex, nodes);
        }

        protected virtual AVLTreeNode createNode() => new AVLTreeNode();
    }

    internal static class AVLTree_API
    {
        internal interface IAVLTreeNode
        {
            public int height { get; set; }

            public int parent_index { get; set; }

            public int left_child_index { get; set; }

            public int right_child_index { get; set; }
        }

        internal const int None = -1;

        internal static int self_balancing<T>(int node_index, T[] nodes) where T : IAVLTreeNode
        {
            if (node_index < 0 || node_index >= nodes.Length)
                return None;
            if (nodes[node_index] != null)
            {
                var left_child_index = nodes[node_index].left_child_index;
                var right_child_index = nodes[node_index].right_child_index;
                var left_height = height(left_child_index, nodes);
                var right_height = height(right_child_index, nodes);
                if (Math.Abs(left_height - right_height) >= 2)
                {
                    if (left_height > right_height)
                    {
                        var child_index_legal = left_child_index >= 0 && left_child_index < nodes.Length;
                        var left_left_height =
                            child_index_legal && nodes[left_child_index].left_child_index != None
                                ? height(nodes[left_child_index].left_child_index, nodes)
                                : 0;
                        var left_right_height =
                            child_index_legal && nodes[left_child_index].right_child_index != None
                                ? height(nodes[left_child_index].right_child_index, nodes)
                                : 0;
                        if (left_left_height > left_right_height)
                        {
                            node_index = right_rot(node_index, nodes);
                        }
                        else
                        {
                            nodes[node_index].left_child_index = left_rot(nodes[node_index].left_child_index, nodes);
                            node_index = right_rot(node_index, nodes);
                        }
                    }
                    else
                    {
                        var child_index_legal = right_child_index >= 0 && right_child_index < nodes.Length;
                        var right_left_height =
                            child_index_legal && nodes[right_child_index].left_child_index != None
                                ? height(nodes[right_child_index].left_child_index, nodes)
                                : 0;
                        var right_right_height =
                            child_index_legal && nodes[right_child_index].right_child_index != None
                                ? height(nodes[right_child_index].right_child_index, nodes)
                                : 0;
                        if (right_right_height >= right_left_height)
                        {
                            node_index = left_rot(node_index, nodes);
                        }
                        else
                        {
                            nodes[node_index].right_child_index = right_rot(nodes[node_index].right_child_index, nodes);
                            node_index = left_rot(node_index, nodes);
                        }
                    }
                }
            }

            return node_index;
        }

        internal static int left_rot<T>(int node_index, T[] nodes) where T : IAVLTreeNode
        {
            var node = nodes[node_index];
            var right_child_index = node.right_child_index;
            var right_child = nodes[right_child_index];
            var right_child_left_child_index = right_child.left_child_index;
            nodes[right_child_index].left_child_index = node_index;
            var parent_index = nodes[node_index].parent_index;
            if (parent_index >= 0 && parent_index < nodes.Length)
            {
                if (nodes[parent_index].left_child_index == node_index)
                {
                    nodes[parent_index].left_child_index = right_child_index;
                }
                else
                {
                    nodes[parent_index].right_child_index = right_child_index;
                }
            }

            nodes[right_child_index].parent_index = parent_index;
            nodes[node_index].parent_index = right_child_index;
            nodes[node_index].right_child_index = right_child_left_child_index;
            if (right_child_left_child_index >= 0 &&
                right_child_left_child_index < nodes.Length &&
                nodes[right_child_left_child_index] != null)
                nodes[right_child_left_child_index].parent_index = node_index;
            udpate_height(node_index, nodes);
            udpate_height(right_child_index, nodes);
            return right_child_index;
        }

        internal static int right_rot<T>(int node_index, T[] nodes) where T : IAVLTreeNode
        {
            var node = nodes[node_index];
            var left_child_index = node.left_child_index;
            var left_child = nodes[left_child_index];
            var left_child_right_child_index = left_child.right_child_index;
            nodes[left_child_index].right_child_index = node_index;
            var parent_index = node.parent_index;
            if (parent_index >= 0 && parent_index < nodes.Length)
            {
                if (nodes[parent_index].left_child_index == node_index)
                {
                    nodes[parent_index].left_child_index = left_child_index;
                }
                else
                {
                    nodes[parent_index].right_child_index = left_child_index;
                }
            }

            nodes[left_child_index].parent_index = parent_index;
            nodes[node_index].parent_index = left_child_index;
            nodes[node_index].left_child_index = left_child_right_child_index;
            if (left_child_right_child_index >= 0 &&
                left_child_right_child_index < nodes.Length &&
                nodes[left_child_right_child_index] != null)
                nodes[left_child_right_child_index].parent_index = node_index;
            udpate_height(node_index, nodes);
            udpate_height(left_child_index, nodes);
            return left_child_index;
        }

        internal static int height<T>(int node_index, T[] nodes) where T : IAVLTreeNode
        {
            var result = 0;
            if (node_index >= 0 && node_index < nodes.Length)
            {
                var node = nodes[node_index];
                if (node != null) result = node.height;
                // var lh = getHeight(node.left_child_index, nodes);
                // var rh = getHeight(node.right_child_index, nodes);
                // result = 1 + Math.Max(lh, rh);
            }

            return result;
        }

        internal static void udpate_height<T>(int node_index, T[] nodes) where T : IAVLTreeNode
        {
            if (node_index < 0 || node_index >= nodes.Length) return;
            var node = nodes[node_index];
            var left_child_index = node.left_child_index;
            var right_chlid_index = node.right_child_index;
            nodes[node_index].height =
                Math.Max(left_child_index != None ? nodes[left_child_index]?.height ?? 0 : 0,
                    right_chlid_index != None ? nodes[right_chlid_index]?.height ?? 0 : 0) + 1;
        }

        internal static void set_parent<T>(int child_index, int parent_index, T[] nodes) where T : IAVLTreeNode
        {
            if (child_index >= 0 && child_index < nodes.Length)
                nodes[child_index].parent_index = parent_index;
        }

        internal static void destroy<T>(int node_index, T[] nodes) where T : IAVLTreeNode
        {
            if (node_index < 0 || node_index >= nodes.Length) return;
            if (nodes[node_index] != null)
            {
                destroy(nodes[node_index].left_child_index, nodes);
                destroy(nodes[node_index].right_child_index, nodes);
                nodes[node_index] = default(T);
            }
        }

        internal static void swap<T>(T[] nodes, int i, int j) where T : IAVLTreeNode
        {
            var node = nodes[i];
            nodes[i] = nodes[j];
            nodes[j] = node;
        }

        internal static void fillup<T>(int removed, int last_index, T[] nodes, ref int rootIndex)
            where T : IAVLTreeNode
        {
            if (removed < last_index)
            {
                var last_node = nodes[last_index];
                swap(nodes, last_index, removed);
                if (last_node.parent_index != None)
                {
                    var parent = nodes[last_node.parent_index];
                    if (parent.left_child_index == last_index)
                    {
                        parent.left_child_index = removed;
                    }
                    else
                    {
                        parent.right_child_index = removed;
                    }
                }

                set_parent(last_node.left_child_index, removed, nodes);
                set_parent(last_node.right_child_index, removed, nodes);

                nodes[last_index] = default(T);

                if (last_index == rootIndex)
                    rootIndex = removed;
            }
        }

        internal static T[] ensure_capacity<T>(T[] nodes, int size) where T : IAVLTreeNode
        {
            var n = nodes.Length;
            if (n >= size) return nodes;
            var newSize = n;
            while (newSize < size)
                newSize = (newSize << 1) + 1;
            var newNodes = new T[newSize];
            Array.Copy(nodes, newNodes, n);
            return newNodes;
        }
    }
}