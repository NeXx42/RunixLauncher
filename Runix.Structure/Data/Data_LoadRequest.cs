public struct LoadingTask
{
    public string header;
    public List<(string, Func<Task>)> task;

    public static LoadingTask Empty => new LoadingTask()
    {
        header = "",
        task = new List<(string, Func<Task>)>()
    };
}