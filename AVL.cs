using System;
using System.Collections.Generic;
using System.Text;

namespace AVL
{
    public abstract class AVLTreeBase<TKey, TValue>
    {
        protected class AVLTreeNode
        {
            public static AVLTreeNode empty = new AVLTreeNode();

            public TKey key;
            public TValue value;

            public int parent_index = InvalidIndex;
            public int left_child_index = InvalidIndex, right_child_index = InvalidIndex;
        }

        protected const int InvalidIndex = -1;

        protected int size;
        protected int rootIndex = -1;

        protected Func<TKey, TKey, int> compareFunc;
        protected AVLTreeNode[] nodes;

        public AVLTreeBase(Func<TKey, TKey, int> compareFunc, int capacity = 32)
        {
            this.compareFunc = compareFunc;
            this.nodes = new AVLTreeNode[Math.Max(32, capacity)];
        }

        public AVLTreeBase(int capacity = 32) : this((x, y) =>
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
            rootIndex = InvalidIndex;
            size = 0;
        }

        protected virtual bool tryDelete(ref int index, TKey key, out TValue value, out int deletedIndex)
        {
            value = default;
            deletedIndex = InvalidIndex;
            if (index < 0 || index >= nodes.Length)
                return false;
            var node = nodes[index];
            if (compareFunc(key, node.key) < 0)
            {
                var left_child_index = node.left_child_index;
                if (tryDelete(ref left_child_index, key, out value, out deletedIndex))
                {
                    node.left_child_index = left_child_index;
                    index = balance(index, nodes);
                    return true;
                }
            }
            else if (compareFunc(key, node.key) > 0)
            {
                var right_child_index = node.right_child_index;
                if (tryDelete(ref right_child_index, key, out value, out deletedIndex))
                {
                    node.right_child_index = right_child_index;
                    index = balance(index, nodes);
                    return true;
                }
            }
            else
            {
                var left_child_index = node.left_child_index;
                var right_child_index = node.right_child_index;
                var has_left = left_child_index != InvalidIndex;
                var has_right = right_child_index != InvalidIndex;
                if (has_left && has_right)
                {
                    var child_index = right_child_index;
                    while (child_index >= 0 && child_index < nodes.Length &&
                           nodes[child_index].left_child_index != InvalidIndex)
                        child_index = nodes[child_index].left_child_index;

                    var child_node = nodes[child_index];
                    var child_key = child_node.key;
                    var child_val = child_node.value;

                    if (tryDelete(ref right_child_index, child_key, out value, out deletedIndex))
                    {
                        node.key = child_key;
                        node.value = child_val;
                        node.right_child_index = right_child_index;
                        index = balance(index, nodes);
                        return true;
                    }
                }
                else
                {
                    var child_index = InvalidIndex;
                    if (has_left)
                    {
                        child_index = left_child_index;
                    }
                    else if (has_right)
                    {
                        child_index = right_child_index;
                    }

                    if (child_index != InvalidIndex)
                    {
                        var child_node = nodes[child_index];
                        var child_left_child_index = child_node.left_child_index;
                        var child_right_child_index = child_node.right_child_index;

                        setParent(child_index, node.parent_index, nodes);

                        swap(nodes, child_index, index);

                        setParent(child_left_child_index, index, nodes);
                        setParent(child_right_child_index, index, nodes);

                        deletedIndex = child_index;
                        value = nodes[deletedIndex].value;
                        nodes[deletedIndex] = null;
                    }
                    else
                    {
                        deletedIndex = index;
                        value = nodes[deletedIndex].value;
                        nodes[deletedIndex] = null;
                        index = InvalidIndex;
                    }

                    return true;
                }
            }

            return false;
        }

        protected virtual int insert(int nodeIndex, TKey key, TValue value, int parentIndex = InvalidIndex)
        {
            if (nodeIndex == InvalidIndex) nodeIndex = size++;

            ensureCapacity(nodeIndex + 1);
            var node = nodes[nodeIndex];
            if (node == null)
            {
                nodes[nodeIndex] = new AVLTreeNode {key = key, value = value, parent_index = parentIndex};
            }
            else if (compareFunc(key, node.key) < 0)
            {
                node.left_child_index = insert(node.left_child_index, key, value, nodeIndex);
                nodeIndex = balance(nodeIndex, nodes);
            }
            else
            {
                node.right_child_index = insert(node.right_child_index, key, value, nodeIndex);
                nodeIndex = balance(nodeIndex, nodes);
            }

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
                if (last_node.parent_index != InvalidIndex)
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

                swap(nodes, last_index, index);

                setParent(last_node.left_child_index, index, nodes);
                setParent(last_node.right_child_index, index, nodes);

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
            var avlTreeNodes = new AVLTreeNode[newSize];
            Array.Copy(nodes, avlTreeNodes, length);
            nodes = avlTreeNodes;
        }

        protected static int balance(int nodeIndex, AVLTreeNode[] nodes)
        {
            if (nodeIndex < 0 || nodeIndex >= nodes.Length)
                return InvalidIndex;
            var node = nodes[nodeIndex];
            if (node != null)
            {
                var left_child_index = node.left_child_index;
                var right_child_index = node.right_child_index;
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
                            node.left_child_index = leftRot(node.left_child_index, nodes);
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
                            node.right_child_index = rightRot(node.right_child_index, nodes);
                            nodeIndex = leftRot(nodeIndex, nodes);
                        }
                    }
                }
            }

            return nodeIndex;
        }

        protected static int leftRot(int nodeIndex, AVLTreeNode[] nodes)
        {
            var node = nodes[nodeIndex];
            var right_child_index = node.right_child_index;
            var right_child = nodes[right_child_index];
            var right_child_left_child_index = right_child.left_child_index;
            right_child.left_child_index = nodeIndex;

            var parent_index = node.parent_index;
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

            right_child.parent_index = parent_index;
            node.parent_index = right_child_index;
            node.right_child_index = right_child_left_child_index;
            // Swap(nodes, right_child_index, nodeIndex);
            if (right_child_left_child_index >= 0 &&
                right_child_left_child_index < nodes.Length &&
                nodes[right_child_left_child_index] != null)
                nodes[right_child_left_child_index].parent_index = nodeIndex;
            return right_child_index;
        }

        protected static int rightRot(int nodeIndex, AVLTreeNode[] nodes)
        {
            var node = nodes[nodeIndex];
            var left_child_index = node.left_child_index;
            var left_child = nodes[left_child_index];
            var left_child_right_child_index = left_child.right_child_index;
            left_child.right_child_index = nodeIndex;
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

            left_child.parent_index = parent_index;
            node.parent_index = left_child_index;
            node.left_child_index = left_child_right_child_index;
            // Swap(nodes, left_child_index, nodeIndex);
            if (left_child_right_child_index >= 0 &&
                left_child_right_child_index < nodes.Length &&
                nodes[left_child_right_child_index] != null)
                nodes[left_child_right_child_index].parent_index = nodeIndex;
            return left_child_index;
        }

        protected static int getHeight(int nodeIndex, AVLTreeNode[] nodes)
        {
            var result = 0;
            if (nodeIndex >= 0 && nodeIndex < nodes.Length)
            {
                var node = nodes[nodeIndex];
                if (node != null)
                {
                    var lh = getHeight(node.left_child_index, nodes);
                    var rh = getHeight(node.right_child_index, nodes);
                    result = 1 + Math.Max(lh, rh);
                }
            }

            return result;
        }

        protected static void setParent(int childIndex, int parentIndex, AVLTreeNode[] nodes)
        {
            if (childIndex >= 0 && childIndex < nodes.Length)
                nodes[childIndex].parent_index = parentIndex;
        }

        protected static void swap(AVLTreeNode[] nodes, int i, int j)
        {
            var node = nodes[i];
            nodes[i] = nodes[j];
            nodes[j] = node;
        }

        private readonly StringBuilder builder = new StringBuilder();

        private readonly Queue<AVLTreeNode> queue = new Queue<AVLTreeNode>();

        public override string ToString()
        {
            builder.Clear();
            Enqueue(rootIndex);

            var height = getHeight(rootIndex, nodes);

            int depth = 0, count = 0;
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                var spaceCount = (((1 << height) - 1) / 2 + 1);
                var fullCount = (1 << depth);
                ++count;
                for (int i = 0; i < spaceCount / fullCount; i++)
                    builder.Append(" ");

                if (node == AVLTreeNode.empty)
                {
                    builder.Append(" * ");
                }
                else
                {
                    builder.Append($"k:{node.key},v:{node.value}");
                }

                if (count == fullCount)
                {
                    depth++;
                    count = 0;
                    builder.Append("\n");
                }

                if (node == AVLTreeNode.empty && depth >= height)
                {
                    continue;
                }

                Enqueue(node.left_child_index);
                Enqueue(node.right_child_index);
            }

            return builder.ToString();
        }

        private void Enqueue(int index)
        {
            AVLTreeNode node = null;
            if (index >= 0 && index < nodes.Length) node = nodes[index];
            queue.Enqueue(node ?? AVLTreeNode.empty);
        }
    }
}