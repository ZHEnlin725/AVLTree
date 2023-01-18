using System;
using System.Collections.Generic;
using System.Text;

namespace AVL
{
   public abstract partial class AVLTreeBase<TValue>
    {
        protected partial class AVLNode : IAVLNode
        {
            public static AVLNode empty = new AVLNode();

            public TValue value;

            public int height { get; set; } = AVL.AVLOp.InvalidIndex;

            public int parent_index { get; set; } = AVLOp.InvalidIndex;

            public int left_child_index { get; set; } = AVLOp.InvalidIndex;

            public int right_child_index { get; set; } = AVLOp.InvalidIndex;
        }

        protected int size;
        protected int rootIndex = AVLOp.InvalidIndex;

        protected Func<TValue, TValue, int> compareFunc;
        protected AVLNode[] nodes;

        public AVLTreeBase(Func<TValue, TValue, int> compareFunc, int capacity = 32)
        {
            this.compareFunc = compareFunc;
            this.nodes = new AVLNode[Math.Max(32, capacity)];
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
                removeAndModifySize(deletedIndex, ref rootIndex);
                return true;
            }

            return default;
        }

        public virtual void Clear()
        {
            destroy(rootIndex);
            rootIndex = AVLOp.InvalidIndex;
            size = 0;
        }

        protected virtual bool tryDelete(ref int index, TValue value, out int deletedIndex)
        {
            deletedIndex = AVLOp.InvalidIndex;
            if (index < 0 || index >= nodes.Length)
                return false;
            var node = nodes[index];
            if (compareFunc(value, node.value) < 0)
            {
                var left_child_index = node.left_child_index;
                if (tryDelete(ref left_child_index, value, out deletedIndex))
                {
                    node.left_child_index = left_child_index;
                    index = AVLOp.balance(index, nodes);
                    return true;
                }
            }
            else if (compareFunc(value, node.value) > 0)
            {
                var right_child_index = node.right_child_index;
                if (tryDelete(ref right_child_index, value, out deletedIndex))
                {
                    node.right_child_index = right_child_index;
                    index = AVLOp.balance(index, nodes);
                    return true;
                }
            }
            else
            {
                var left_child_index = node.left_child_index;
                var right_child_index = node.right_child_index;
                var has_left = left_child_index != AVLOp.InvalidIndex;
                var has_right = right_child_index != AVLOp.InvalidIndex;
                if (has_left && has_right)
                {
                    var child_index = right_child_index;
                    while (child_index >= 0 && child_index < nodes.Length &&
                           nodes[child_index].left_child_index != AVLOp.InvalidIndex)
                        child_index = nodes[child_index].left_child_index;

                    var child_node = nodes[child_index];
                    var child_val = child_node.value;

                    if (tryDelete(ref right_child_index, value, out deletedIndex))
                    {
                        node.value = child_val;
                        node.right_child_index = right_child_index;
                        index = AVLOp.balance(index, nodes);
                        return true;
                    }
                }
                else
                {
                    var child_index = AVLOp.InvalidIndex;
                    if (has_left)
                    {
                        child_index = left_child_index;
                    }
                    else if (has_right)
                    {
                        child_index = right_child_index;
                    }

                    if (child_index != AVLOp.InvalidIndex)
                    {
                        var child_node = nodes[child_index];
                        var child_left_child_index = child_node.left_child_index;
                        var child_right_child_index = child_node.right_child_index;

                        AVLOp.setParent(child_index, node.parent_index, nodes);

                        AVLOp.swap(nodes, child_index, index);

                        AVLOp.setParent(child_left_child_index, index, nodes);
                        AVLOp.setParent(child_right_child_index, index, nodes);

                        deletedIndex = child_index;
                        nodes[deletedIndex] = null;
                    }
                    else
                    {
                        deletedIndex = index;
                        nodes[deletedIndex] = null;
                        index = AVLOp.InvalidIndex;
                    }

                    return true;
                }
            }

            return false;
        }

        protected virtual int insert(int nodeIndex, TValue value, int parentIndex = AVLOp.InvalidIndex)
        {
            if (nodeIndex == AVLOp.InvalidIndex) nodeIndex = size++;

            ensureCapacity(nodeIndex + 1);
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
                nodeIndex = AVLOp.balance(nodeIndex, nodes);
            }
            else
            {
                node.right_child_index = insert(node.right_child_index, value, nodeIndex);
                nodeIndex = AVLOp.balance(nodeIndex, nodes);
            }

            AVLOp.updateHeight(nodeIndex, nodes);
            return nodeIndex;
        }

        protected virtual void destroy(int nodeIndex)
        {
            if (nodeIndex >= 0 && nodeIndex < nodes.Length)
            {
                var node = nodes[nodeIndex];
                if (node != null)
                {
                    destroy(node.left_child_index);
                    destroy(node.right_child_index);
                }

                nodes[nodeIndex] = null;
            }
        }

        protected virtual void removeAndModifySize(int index, ref int rootIndex)
        {
            var last_index = --size;
            if (index < last_index)
            {
                var last_node = nodes[last_index];
                if (last_node.parent_index != AVLOp.InvalidIndex)
                {
                    var parent = nodes[last_node.parent_index];
                    if (parent.left_child_index == last_index)
                    {
                        parent.left_child_index = index;
                    }
                    else
                    {
                        parent.right_child_index = index;
                    }
                }

                AVLOp.swap(nodes, last_index, index);

                AVLOp.setParent(last_node.left_child_index, index, nodes);
                AVLOp.setParent(last_node.right_child_index, index, nodes);

                nodes[last_index] = null;

                if (last_index == rootIndex)
                    rootIndex = index;
            }
        }

        protected virtual void ensureCapacity(int size)
        {
            var length = nodes.Length;
            if (length >= size) return;
            var newSize = length;
            while (newSize < size)
                newSize = (newSize << 1) + 1;
            var avlTreeNodes = new AVLNode[newSize];
            Array.Copy(nodes, avlTreeNodes, length);
            nodes = avlTreeNodes;
        }

        protected virtual AVLNode createNode() => new AVLNode();
    }

    public abstract partial class AVLTreeBase_KV<TKey, TValue>
    {
        protected partial class AVLNode : IAVLNode
        {
            public static AVLNode empty = new AVLNode();

            public TKey key;
            public TValue value;

            public int height { get; set; }

            public int parent_index { get; set; } = AVLOp.InvalidIndex;

            public int left_child_index { get; set; } = AVLOp.InvalidIndex;

            public int right_child_index { get; set; } = AVLOp.InvalidIndex;
        }

        protected int size;
        protected int rootIndex = AVLOp.InvalidIndex;

        protected Func<TKey, TKey, int> compareFunc;
        protected AVLNode[] nodes;

        public AVLTreeBase_KV(Func<TKey, TKey, int> compareFunc, int capacity = 32)
        {
            this.compareFunc = compareFunc;
            this.nodes = new AVLNode[Math.Max(32, capacity)];
        }

        public AVLTreeBase_KV(int capacity = 32) : this((x, y) =>
            ((IComparable<TKey>) x).CompareTo(y), capacity)
        {
            if (!typeof(IComparable<TKey>).IsAssignableFrom(typeof(TKey)))
                throw new ArgumentException($"{typeof(IComparable)} Must Assignable From {typeof(TKey)}");
        }

        public virtual void Insert(TKey key, TValue value)
        {
            if (key == null) return;
            rootIndex = insert(rootIndex, key, value);
        }

        public virtual TValue Query(TKey key)
        {
            if (key == null) return default;
            return query(rootIndex, key);
        }

        public virtual bool Delete(TKey key, out TValue value)
        {
            value = default;
            if (key == null) return false;
            var index = rootIndex;
            if (tryDelete(ref index, key, out value, out var deletedIndex))
            {
                rootIndex = index;
                removeAndModifySize(deletedIndex, ref rootIndex);
                return true;
            }

            return default;
        }

        public virtual void Clear()
        {
            destroy(rootIndex);
            rootIndex = AVLOp.InvalidIndex;
            size = 0;
        }

        protected virtual bool tryDelete(ref int index, TKey key, out TValue value, out int deletedIndex)
        {
            value = default;
            deletedIndex = AVLOp.InvalidIndex;
            if (index < 0 || index >= nodes.Length)
                return false;
            var node = nodes[index];
            if (compareFunc(key, node.key) < 0)
            {
                var left_child_index = node.left_child_index;
                if (tryDelete(ref left_child_index, key, out value, out deletedIndex))
                {
                    node.left_child_index = left_child_index;
                    index = AVLOp.balance(index, nodes);
                    return true;
                }
            }
            else if (compareFunc(key, node.key) > 0)
            {
                var right_child_index = node.right_child_index;
                if (tryDelete(ref right_child_index, key, out value, out deletedIndex))
                {
                    node.right_child_index = right_child_index;
                    index = AVLOp.balance(index, nodes);
                    return true;
                }
            }
            else
            {
                var left_child_index = node.left_child_index;
                var right_child_index = node.right_child_index;
                var has_left = left_child_index != AVLOp.InvalidIndex;
                var has_right = right_child_index != AVLOp.InvalidIndex;
                if (has_left && has_right)
                {
                    var child_index = right_child_index;
                    while (child_index >= 0 && child_index < nodes.Length &&
                           nodes[child_index].left_child_index != AVLOp.InvalidIndex)
                        child_index = nodes[child_index].left_child_index;

                    var child_node = nodes[child_index];
                    var child_key = child_node.key;
                    var child_val = child_node.value;

                    if (tryDelete(ref right_child_index, child_key, out value, out deletedIndex))
                    {
                        node.key = child_key;
                        node.value = child_val;
                        node.right_child_index = right_child_index;
                        index = AVLOp.balance(index, nodes);
                        return true;
                    }
                }
                else
                {
                    var child_index = AVLOp.InvalidIndex;
                    if (has_left)
                    {
                        child_index = left_child_index;
                    }
                    else if (has_right)
                    {
                        child_index = right_child_index;
                    }

                    if (child_index != AVLOp.InvalidIndex)
                    {
                        var child_node = nodes[child_index];
                        var child_left_child_index = child_node.left_child_index;
                        var child_right_child_index = child_node.right_child_index;

                        AVLOp.setParent(child_index, node.parent_index, nodes);

                        AVLOp.swap(nodes, child_index, index);

                        AVLOp.setParent(child_left_child_index, index, nodes);
                        AVLOp.setParent(child_right_child_index, index, nodes);

                        deletedIndex = child_index;
                        value = nodes[deletedIndex].value;
                        nodes[deletedIndex] = null;
                    }
                    else
                    {
                        deletedIndex = index;
                        value = nodes[deletedIndex].value;
                        nodes[deletedIndex] = null;
                        index = AVLOp.InvalidIndex;
                    }

                    return true;
                }
            }

            return false;
        }

        protected virtual TValue query(int index, TKey key)
        {
            if (index < 0 || index >= nodes.Length)
                return default;
            var node = nodes[index];
            var compareVal = compareFunc(key, node.key);
            if (compareVal < 0)
            {
                return query(node.left_child_index, key);
            }

            if (compareVal > 0)
            {
                return query(node.right_child_index, key);
            }

            return node.value;
        }

        protected virtual int insert(int nodeIndex, TKey key, TValue value, int parentIndex = AVLOp.InvalidIndex)
        {
            if (nodeIndex == AVLOp.InvalidIndex) nodeIndex = size++;

            ensureCapacity(nodeIndex + 1);
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
                nodeIndex = AVLOp.balance(nodeIndex, nodes);
            }
            else
            {
                node.right_child_index = insert(node.right_child_index, key, value, nodeIndex);
                nodeIndex = AVLOp.balance(nodeIndex, nodes);
            }

            AVLOp.updateHeight(nodeIndex, nodes);
            return nodeIndex;
        }

        protected virtual void destroy(int nodeIndex)
        {
            if (nodeIndex >= 0 && nodeIndex < nodes.Length)
            {
                var node = nodes[nodeIndex];
                if (node != null)
                {
                    destroy(node.left_child_index);
                    destroy(node.right_child_index);
                }

                nodes[nodeIndex] = null;
            }
        }

        protected virtual void removeAndModifySize(int index, ref int rootIndex)
        {
            var last_index = --size;
            if (index < last_index)
            {
                var last_node = nodes[last_index];
                if (last_node.parent_index != AVLOp.InvalidIndex)
                {
                    var parent = nodes[last_node.parent_index];
                    if (parent.left_child_index == last_index)
                    {
                        parent.left_child_index = index;
                    }
                    else
                    {
                        parent.right_child_index = index;
                    }
                }

                AVLOp.swap(nodes, last_index, index);

                AVLOp.setParent(last_node.left_child_index, index, nodes);
                AVLOp.setParent(last_node.right_child_index, index, nodes);

                nodes[last_index] = null;

                if (last_index == rootIndex)
                    rootIndex = index;
            }
        }

        protected virtual void ensureCapacity(int size)
        {
            var length = nodes.Length;
            if (length >= size) return;
            var newSize = length;
            while (newSize < size)
                newSize = (newSize << 1) + 1;
            var avlTreeNodes = new AVLNode[newSize];
            Array.Copy(nodes, avlTreeNodes, length);
            nodes = avlTreeNodes;
        }

        protected virtual AVLNode createNode() => new AVLNode();
    }

    public interface IAVLNode
    {
        public int height { get; set; }

        public int parent_index { get; set; }

        public int left_child_index { get; set; }

        public int right_child_index { get; set; }
    }

    internal static class AVLOp
    {
        public const int InvalidIndex = -1;

        public static int balance(int nodeIndex, IAVLNode[] nodes)
        {
            if (nodeIndex < 0 || nodeIndex >= nodes.Length)
                return InvalidIndex;
            if (nodes[nodeIndex] != null)
            {
                var left_child_index = nodes[nodeIndex].left_child_index;
                var right_child_index = nodes[nodeIndex].right_child_index;
                var left_height = getHeight(left_child_index, nodes);
                var right_height = getHeight(right_child_index, nodes);
                if (Math.Abs(left_height - right_height) >= 2)
                {
                    if (left_height > right_height)
                    {
                        var child_index_legal = left_child_index >= 0 && left_child_index < nodes.Length;
                        var left_left_height =
                            child_index_legal && nodes[left_child_index].left_child_index != InvalidIndex
                                ? getHeight(nodes[left_child_index].left_child_index, nodes)
                                : 0;
                        var left_right_height =
                            child_index_legal && nodes[left_child_index].right_child_index != InvalidIndex
                                ? getHeight(nodes[left_child_index].right_child_index, nodes)
                                : 0;
                        if (left_left_height > left_right_height)
                        {
                            nodeIndex = rightRot(nodeIndex, nodes);
                        }
                        else
                        {
                            nodes[nodeIndex].left_child_index = leftRot(nodes[nodeIndex].left_child_index, nodes);
                            nodeIndex = rightRot(nodeIndex, nodes);
                        }
                    }
                    else
                    {
                        var child_index_legal = right_child_index >= 0 && right_child_index < nodes.Length;
                        var right_left_height =
                            child_index_legal && nodes[right_child_index].left_child_index != InvalidIndex
                                ? getHeight(nodes[right_child_index].left_child_index, nodes)
                                : 0;
                        var right_right_height =
                            child_index_legal && nodes[right_child_index].right_child_index != InvalidIndex
                                ? getHeight(nodes[right_child_index].right_child_index, nodes)
                                : 0;
                        if (right_right_height >= right_left_height)
                        {
                            nodeIndex = leftRot(nodeIndex, nodes);
                        }
                        else
                        {
                            nodes[nodeIndex].right_child_index = rightRot(nodes[nodeIndex].right_child_index, nodes);
                            nodeIndex = leftRot(nodeIndex, nodes);
                        }
                    }
                }
            }

            return nodeIndex;
        }

        public static int leftRot(int nodeIndex, IAVLNode[] nodes)
        {
            var node = nodes[nodeIndex];
            var right_child_index = node.right_child_index;
            var right_child = nodes[right_child_index];
            var right_child_left_child_index = right_child.left_child_index;
            nodes[right_child_index].left_child_index = nodeIndex;
            var parent_index = nodes[nodeIndex].parent_index;
            if (parent_index >= 0 && parent_index < nodes.Length)
            {
                if (nodes[parent_index].left_child_index == nodeIndex)
                {
                    nodes[parent_index].left_child_index = right_child_index;
                }
                else
                {
                    nodes[parent_index].right_child_index = right_child_index;
                }
            }

            nodes[right_child_index].parent_index = parent_index;
            nodes[nodeIndex].parent_index = right_child_index;
            nodes[nodeIndex].right_child_index = right_child_left_child_index;
            if (right_child_left_child_index >= 0 &&
                right_child_left_child_index < nodes.Length &&
                nodes[right_child_left_child_index] != null)
                nodes[right_child_left_child_index].parent_index = nodeIndex;
            updateHeight(nodeIndex, nodes);
            updateHeight(right_child_index, nodes);
            return right_child_index;
        }

        public static int rightRot(int nodeIndex, IAVLNode[] nodes)
        {
            var node = nodes[nodeIndex];
            var left_child_index = node.left_child_index;
            var left_child = nodes[left_child_index];
            var left_child_right_child_index = left_child.right_child_index;
            nodes[left_child_index].right_child_index = nodeIndex;
            var parent_index = node.parent_index;
            if (parent_index >= 0 && parent_index < nodes.Length)
            {
                if (nodes[parent_index].left_child_index == nodeIndex)
                {
                    nodes[parent_index].left_child_index = left_child_index;
                }
                else
                {
                    nodes[parent_index].right_child_index = left_child_index;
                }
            }

            nodes[left_child_index].parent_index = parent_index;
            nodes[nodeIndex].parent_index = left_child_index;
            nodes[nodeIndex].left_child_index = left_child_right_child_index;
            if (left_child_right_child_index >= 0 &&
                left_child_right_child_index < nodes.Length &&
                nodes[left_child_right_child_index] != null)
                nodes[left_child_right_child_index].parent_index = nodeIndex;
            updateHeight(nodeIndex, nodes);
            updateHeight(left_child_index, nodes);
            return left_child_index;
        }

        public static int getHeight(int nodeIndex, IAVLNode[] nodes)
        {
            var result = 0;
            if (nodeIndex >= 0 && nodeIndex < nodes.Length)
            {
                var node = nodes[nodeIndex];
                if (node != null) result = node.height;
                // var lh = getHeight(node.left_child_index, nodes);
                // var rh = getHeight(node.right_child_index, nodes);
                // result = 1 + Math.Max(lh, rh);
            }

            return result;
        }

        public static void updateHeight(int nodeIndex, IAVLNode[] nodes)
        {
            if (nodeIndex < 0 || nodeIndex >= nodes.Length) return;
            var node = nodes[nodeIndex];
            var left_child_index = node.left_child_index;
            var right_chlid_index = node.right_child_index;
            nodes[nodeIndex].height =
                Math.Max(left_child_index != InvalidIndex ? nodes[left_child_index]?.height ?? 0 : 0,
                    right_chlid_index != InvalidIndex ? nodes[right_chlid_index]?.height ?? 0 : 0) + 1;
        }

        public static void setParent(int childIndex, int parentIndex, IAVLNode[] nodes)
        {
            if (childIndex >= 0 && childIndex < nodes.Length)
                nodes[childIndex].parent_index = parentIndex;
        }

        public static void swap(IAVLNode[] nodes, int i, int j)
        {
            var node = nodes[i];
            nodes[i] = nodes[j];
            nodes[j] = node;
        }
    }
}