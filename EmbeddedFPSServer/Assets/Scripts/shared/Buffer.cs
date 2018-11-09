using System.Collections.Generic;

public class Buffer<T>
{

    public int Size { get { return elements.Count; } }

    private Queue<T> elements;
    private int bufferState;
    private int maxElementsPerGet;


    public Buffer(int bufferSize, int maxElementsPerGet)
    {
        elements = new Queue<T>();
        bufferState = -bufferSize;
        this.maxElementsPerGet = maxElementsPerGet - 1;
    }


    public void Add(T element)
    {
        elements.Enqueue(element);
    }

    public T[] Get()
    {
        if (bufferState < 0)
        {
            bufferState++;
            return new T[0];
        }

        if (elements.Count == 0)
        {
            bufferState++;
            return new T[0];
        }

        if (bufferState == 0)
        {
            return new T[] { elements.Dequeue() };
        }

        int amountToGet = bufferState > maxElementsPerGet ? maxElementsPerGet : bufferState;
        bufferState = 0;
        T[] val = new T[amountToGet + 1];
        for (int i = amountToGet; i >= 0; i--)
        {
            val[i] = elements.Dequeue();
        }

        return val;

    }
}

