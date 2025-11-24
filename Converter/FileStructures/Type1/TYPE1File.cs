namespace Converter.FileStructures.Type1
{
  // ROOT
  public class TYPE1_Font
  {
    public TYPE1_FontDict FontDict;
    public TYPE1_Comments Comments;
  }

  public class TYPE1_Comments
  {
    public TYPE1_Header Header;
    public TYPE1_Body Body;
    public TYPE1_Page Page; // THis should be a list
  }
  public class TYPE1_Header
  {

  }

  public class TYPE1_Body
  {
  }

  public class TYPE1_Page
  {
  }
}
