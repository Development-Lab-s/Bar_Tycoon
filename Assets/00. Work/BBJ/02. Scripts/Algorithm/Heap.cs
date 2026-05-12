using System;
namespace Algorithm
{
    public class Heap<T> where T : IHeapItem<T>
    {
        T[] items;
        int currentItemCount = 0;

        public Heap(int maxHeapSize)
        {
            items = new T[maxHeapSize + 1]; // 0은 사용하지 않음
        }
        public void Add(T item)
        {
            // 가장 뒤에 추가후 정렬
            currentItemCount++;
            item.HeapIndex = currentItemCount;
            items[currentItemCount] = item;
            SortUp(item);
        }
        public T RemoveFirst()
        {
            // Pop 하고 정렬
            T firstItem = items[1];
            items[1] = items[currentItemCount];
            items[1].HeapIndex = 1;
            currentItemCount--;
            SortDown(items[1]);
            return firstItem;
        }
        public int Count => currentItemCount;
        public bool Contains(T item) => Equals(items[item.HeapIndex], item);
        public void UpdateItem(T item) => SortUp(item);

        // 자식노드 이진탐색해서 업데이트
        void SortDown(T item)
        {
            while (true)
            {
                //Index로 하는 이유 => T는 추상적이라서 혹시 엄청 큰 구조체를 복사할 수도 있고 깊은 복사가 안되면 Set을 못하고

                int childIndexLeft = item.HeapIndex * 2;
                int childIndexRight = item.HeapIndex * 2 + 1;
                int swapIndex;
                if (childIndexLeft <= currentItemCount)
                {
                    swapIndex = childIndexLeft;

                    if (childIndexRight <= currentItemCount)
                    {
                        if (items[childIndexLeft].CompareTo(items[childIndexRight]) < 0)
                            swapIndex = childIndexRight;
                    }
                    if (item.CompareTo(items[swapIndex]) < 0)
                        Swap(item, items[swapIndex]);
                    else return;
                }
                else return;
            }
        }
        // 부모노드 이진탐색해서 업데이트
        void SortUp(T item)
        {
            int parentIndex = item.HeapIndex / 2;

            while (parentIndex != 0)
            {
                T parentItem = items[parentIndex];
                if (item.CompareTo(items[parentIndex]) > 0)
                    Swap(item, parentItem);
                else
                    break;
                parentIndex = item.HeapIndex / 2;
            }
        }

        void Swap(T itemA, T itemB)
        {
            items[itemA.HeapIndex] = itemB;
            items[itemB.HeapIndex] = itemA;

            int swapIndex = itemA.HeapIndex;
            itemA.HeapIndex = itemB.HeapIndex;
            itemB.HeapIndex = swapIndex;
        }

    }
}