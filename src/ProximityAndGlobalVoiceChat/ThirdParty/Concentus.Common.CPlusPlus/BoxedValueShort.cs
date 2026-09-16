namespace Concentus.Common.CPlusPlus;

internal class BoxedValueShort
{
	public short Val;

	public BoxedValueShort(short v = 0)
	{
		Val = v;
	}

	public override string ToString()
	{
		return Val.ToString();
	}
}
