namespace Converter.StaticData
{
  public static class TYPE1Constants
  {
    // I use these in TYPE1 Parser helper but in reality these are global character bytes
    // I should prob move them somewhere else
    public static byte LEFT_BRACKET = (byte)'{';
    public static byte RIGHT_BRACKET = (byte)'}';
    public static byte LEFT_PARENTHESIS = (byte)'(';
    public static byte RIGHT_PARENTHESIS = (byte)')';
    public static byte SLASH = (byte)'/';
    public static byte LESS_THAN = (byte)'<';
    public static byte MORE_THAN = (byte)'>';
  }
}
