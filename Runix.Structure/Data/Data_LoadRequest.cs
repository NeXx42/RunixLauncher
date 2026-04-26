public struct LoadingTask
{
    public string header;
    public List<(string, Func<Task>)> task;

    public static LoadingTask Empty => new LoadingTask()
    {
        header = "",
        task = new List<(string, Func<Task>)>()
    };

    public LoadingTask(string header, string description, Func<Task> task)
    {
        this.header = header;
        this.task = new List<(string, Func<Task>)>()
        {
            (description, task)
        };
    }
}