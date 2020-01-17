public class OpenCVInputBuffer<T>
{
	public int MaxSize;
	public int curLength;
	public T[] data;

	public OpenCVInputBuffer(int size_)
	{
		MaxSize = size_;
		curLength = 0;
		data = new T[MaxSize];
	}

	public void push_back(T t)
	{
		if (curLength < MaxSize - 1)
		{
			data [curLength] = t;
			curLength++;
		}
		else
		{
			for (int i = 0; i < MaxSize - 1; i++)
				data [i] = data [i + 1];
			data[curLength] = t;
		}
	}

//	T getAverage() // Cannot exist ... Because C# 
//	{
//		T tmp;
//		for (int i = 0; i < curLength - 1; i++)
//			tmp = tmp + data [i];
//		return tmp;
//	}
}