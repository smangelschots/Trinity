namespace Trinity
{
    public class EditEventHandlerArgs
    {
        public EditType EditType { get; set; }

        public EditEventHandlerArgs(EditType editType)
        {
            this.EditType = editType;
        }
    }
}