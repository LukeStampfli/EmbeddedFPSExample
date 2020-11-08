using System.Collections.Generic;
using System.Linq;

public class Buffer<T>
{
    private Queue<T> elements;
    private int bufferSize;
    private int counter;
    private int correctionTollerance;

    public Buffer(int bufferSize, int correctionTollerance)
    {
        this.bufferSize = bufferSize;
        this.correctionTollerance = correctionTollerance;
        elements = new Queue<T>();
    }

    public int Count => elements.Count;

    public void Add(T element)
    {
        elements.Enqueue(element);
    }

    public T[] Get()
    {
        int size = elements.Count - 1;

        if (size == bufferSize)
        {
            counter = 0;
        }

        if (size > bufferSize)
        {
            if (counter < 0)
            {
                counter = 0;
            }
            counter++;
            if (counter > correctionTollerance)
            {
                int amount = elements.Count - bufferSize;
                T[] temp = new T[amount];
                for (int i = 0; i < amount; i++)
                {
                    temp[i] = elements.Dequeue();
                }

                return temp;
            }
        }

        if (size < bufferSize)
        {
            if (counter > 0)
            {
                counter = 0;
            }
            counter--;
            if (-counter > correctionTollerance)
            {
                return new T[0];
            }
        }

        if (elements.Any())
        {
            return new T[] { elements.Dequeue() };
        }
        return new T[0];
    }
}

