public class OpenCVInputBuffer<T>
{
    public int maxSize;
    public int curLength;
    public T[] data;

    public OpenCVInputBuffer(int size)
    {
        maxSize = size;
        curLength = 0;
        data = new T[maxSize];
    }

    public void PushBack(T t)
    {
        if (curLength < maxSize - 1)
        {
            data[curLength] = t;
            curLength++;
        }
        else
        {
            for (int i = 0; i < maxSize - 1; i++)
                data[i] = data[i + 1];
            data[curLength] = t;
        }
    }

}
