using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class HuffmanCompression
{
    static void Main()
    {
        string filePath = "Hobbit.txt";
        string compressedFilePath = "Hobbit.архив";

        // Чтение данных из файла
        byte[] data = File.ReadAllBytes(filePath);

        // Получение словаря частот байтов
        Dictionary<byte, int> frequency = GetByteFrequency(data);

        // Построение дерева Хаффмана
        HuffmanNode huffmanTree = BuildHuffmanTree(frequency);

        // Получение кодов Хаффмана для каждого байта
        Dictionary<byte, List<bool>> huffmanCodes = BuildHuffmanCodes(huffmanTree);

        // Кодирование данных
        List<bool> encodedData = EncodeData(data, huffmanCodes);

        // Сохранение закодированных данных в бинарный файл
        SaveToFile(compressedFilePath, encodedData);

        Console.WriteLine("Архивация завершена.");
    }

    static Dictionary<byte, int> GetByteFrequency(byte[] data)
    {
        return data.GroupBy(b => b)
                   .ToDictionary(g => g.Key, g => g.Count());
    }

    static HuffmanNode BuildHuffmanTree(Dictionary<byte, int> frequency)
    {
        PriorityQueue<HuffmanNode> priorityQueue = new PriorityQueue<HuffmanNode>();

        foreach (var kvp in frequency)
        {
            priorityQueue.Enqueue(new HuffmanNode(kvp.Key, kvp.Value));
        }

        while (priorityQueue.Count > 1)
        {
            HuffmanNode left = priorityQueue.Dequeue();
            HuffmanNode right = priorityQueue.Dequeue();

            HuffmanNode parent = new HuffmanNode(0, left.Frequency + right.Frequency);
            parent.Left = left;
            parent.Right = right;

            priorityQueue.Enqueue(parent);
        }

        return priorityQueue.Dequeue();
    }

    static Dictionary<byte, List<bool>> BuildHuffmanCodes(HuffmanNode root)
    {
        Dictionary<byte, List<bool>> huffmanCodes = new Dictionary<byte, List<bool>>();
        BuildHuffmanCodesRecursive(root, new List<bool>(), huffmanCodes);
        return huffmanCodes;
    }

    static void BuildHuffmanCodesRecursive(HuffmanNode node, List<bool> code, Dictionary<byte, List<bool>> huffmanCodes)
    {
        if (node.IsLeaf())
        {
            huffmanCodes[node.ByteValue] = new List<bool>(code);
        }
        else
        {
            if (node.Left != null)
            {
                code.Add(false);
                BuildHuffmanCodesRecursive(node.Left, code, huffmanCodes);
                code.RemoveAt(code.Count - 1);
            }

            if (node.Right != null)
            {
                code.Add(true);
                BuildHuffmanCodesRecursive(node.Right, code, huffmanCodes);
                code.RemoveAt(code.Count - 1);
            }
        }
    }

    static List<bool> EncodeData(byte[] data, Dictionary<byte, List<bool>> huffmanCodes)
    {
        List<bool> encodedData = new List<bool>();

        foreach (byte b in data)
        {
            encodedData.AddRange(huffmanCodes[b]);
        }

        return encodedData;
    }

    static void SaveToFile(string filePath, List<bool> data)
    {
        using (FileStream stream = new FileStream(filePath, FileMode.Create))
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            // Запись битов в файл
            byte currentByte = 0;
            int bitIndex = 7;

            foreach (bool bit in data)
            {
                if (bit)
                {
                    currentByte |= (byte)(1 << bitIndex);
                }

                bitIndex--;

                if (bitIndex == -1)
                {
                    writer.Write(currentByte);
                    currentByte = 0;
                    bitIndex = 7;
                }
            }

            // Запись последнего байта (если необходимо)
            if (bitIndex != 7)
            {
                writer.Write(currentByte);
            }
        }
    }

    class HuffmanNode : IComparable<HuffmanNode>
    {
        public byte ByteValue { get; set; }
        public int Frequency { get; set; }
        public HuffmanNode Left { get; set; }
        public HuffmanNode Right { get; set; }

        public HuffmanNode(byte byteValue, int frequency)
        {
            ByteValue = byteValue;
            Frequency = frequency;
        }

        public bool IsLeaf()
        {
            return Left == null && Right == null;
        }

        public int CompareTo(HuffmanNode other)
        {
            return Frequency.CompareTo(other.Frequency);
        }
    }

    class PriorityQueue<T> where T : IComparable<T>
    {
        private List<T> heap;

        public int Count
        {
            get { return heap.Count; }
        }

        public PriorityQueue()
        {
            heap = new List<T>();
        }

        public void Enqueue(T item)
        {
            heap.Add(item);
            int i = heap.Count - 1;
            while (i > 0)
            {
                int parent = (i - 1) / 2;
                if (heap[i].CompareTo(heap[parent]) >= 0)
                    break;

                Swap(i, parent);
                i = parent;
            }
        }

        public T Dequeue()
        {
            if (Count == 0)
                throw new InvalidOperationException("Priority queue is empty");

            T root = heap[0];
            int last = heap.Count - 1;
            heap[0] = heap[last];
            heap.RemoveAt(last);

            int current = 0;
            while (true)
            {
                int leftChild = 2 * current + 1;
                int rightChild = 2 * current + 2;

                if (leftChild >= Count)
                    break;

                int child = (rightChild >= Count || heap[leftChild].CompareTo(heap[rightChild]) <= 0)
                    ? leftChild
                    : rightChild;

                if (heap[current].CompareTo(heap[child]) <= 0)
                    break;

                Swap(current, child);
                current = child;
            }

            return root;
        }

        private void Swap(int i, int j)
        {
            T temp = heap[i];
            heap[i] = heap[j];
            heap[j] = temp;
        }
    }
}
