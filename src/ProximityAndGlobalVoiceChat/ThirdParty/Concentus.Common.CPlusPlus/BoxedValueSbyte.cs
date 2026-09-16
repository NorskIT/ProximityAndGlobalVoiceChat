namespace Concentus.Common.CPlusPlus;

internal class BoxedValueSbyte
{
	public sbyte Val;

	public BoxedValueSbyte(sbyte v = 0)
	{
		Val = v;
	}

	public override string ToString()
	{
		return Val.ToString();
	}
}
