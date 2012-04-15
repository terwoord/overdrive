using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TerWoord.OverDriveStorage.Legacy.Utilities
{
    public sealed class RawLinkedList<T>
    {
        public class Node
        {
            internal Node(T value)
            {
                Value = value;
            }
            public readonly T Value;
            public Node Previous;
            public Node Next;
        }

        public Node First;
        public Node Last;
        public uint Count;

        public void Remove(Node node)
        {
            if (node.Next == null && node.Previous==null)
            {
                // node is only node
                First = Last = null;
                Count = 0;
                return;
            }
            if (node.Next == null)
            {
                // item is last item
                node.Previous.Next = null;
                Last = node.Previous;
                Count--;
                return;
            }
            if (node.Previous == null)
            {
                // node is first item
                node.Next.Previous = null;
                First = node.Next;
                Count--;
                return;
            }
            node.Previous.Next = node.Next;
            node.Next.Previous = node.Previous;
            Count--;
        }

        public void RemoveLast()
        {
            if (Count != 1)
            {
                Last.Previous.Next = null;
                Last = Last.Previous;
                Count--;
            }
            else
            {
                Last = First = null;
                Count = 0;
            }
        }

        public void AddLast(T value)
        {
            AddLast(new Node(value));
        }

        public void AddLast(Node node)
        {
            if (First == null)
            {
                // contains zero items
                First = Last = node;
                Count = 1;
                return;
            }
            Last.Next = node;
            node.Previous = Last;
            Last = node;
            Count++;
        }

        public void AddFirst(T value)
        {
            AddFirst(new Node(value));
        }

        public void AddFirst(Node node)
        {
            if (First == null)
            {
                // contains zero items
                First = Last = node;
                Count = 1;
                return;
            }
            First.Previous = node;
            node.Next = First;
            First = node;
            Count++;
        }

        public void Clear()
        {
            First = null;
            Last = null;
            Count = 0;
        }

        public void MoveFirst(Node node)
        {
            if (Count == 0)
            {
                throw new Exception("No items in list!");
            }
            // remove item from old position
            if (node.Previous != null && node.Next != null)
            {
                node.Previous.Next = node.Next.Previous;
            }
            else
            {
                if (node.Previous == null)
                {
                    // already first?
                    return;
                }
                else
                {
                    //node.Next == null
                    node.Previous.Next = null;
                }
            }
            First.Previous = node;            
            node.Next = First;
            node.Previous = null;
            First = node;
        }
    }
}
