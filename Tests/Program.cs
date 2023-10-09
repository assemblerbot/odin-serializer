// See https://aka.ms/new-console-template for more information

using OdinSerializer;

[Serializable]
public class Test1
{
	public int IntValue;

	public void Init()
	{
		IntValue = 3;
	}
}

public class Program
{
	public static void Main(string[] args)
	{
		Console.WriteLine("Serialize");
		
		Test1 test1_in = new();
		test1_in.Init();
		byte[] json = SerializationUtility.SerializeValue(test1_in, DataFormat.JSON);
		Console.WriteLine(System.Text.Encoding.UTF8.GetString(json));
		
		Console.WriteLine("Deserialize");
		
		Test1 test1_out = SerializationUtility.DeserializeValue<Test1>(json, DataFormat.JSON);
		
		Console.WriteLine("Done");
	}
}

