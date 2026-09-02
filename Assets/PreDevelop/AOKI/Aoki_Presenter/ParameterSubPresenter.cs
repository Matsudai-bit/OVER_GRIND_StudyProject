public class ParameterSubPresenter : ISubPresenter
{
    private readonly ParameterModel _model;
    private readonly ValueTextView _view;

    public ParameterSubPresenter(ParameterModel model, ValueTextView view)
    {
        _model = model;
        _view = view;
    }

    public void Bind()
    {
        UpdateView();
        _model.OnValueChanged += UpdateView;
    }

    public void Unbind()
    {
        _model.OnValueChanged -= UpdateView;
    }

    private void UpdateView()
    {
        _view.Render(_model.Name, _model.GetValueString());
    }
}