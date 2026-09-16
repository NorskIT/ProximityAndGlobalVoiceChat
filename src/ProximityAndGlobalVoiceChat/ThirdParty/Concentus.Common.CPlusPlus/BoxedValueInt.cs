namespace Concentus.Common.CPlusPlus;

internal class BoxedValueInt
{
	public int Val;

	public BoxedValueInt(int v = 0)
	{
		Val = v;
	}

	public override string ToString()
	{
		return Val.ToString();
	}
}
