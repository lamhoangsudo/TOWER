using System;
using System.Threading.Tasks;

namespace Nav3D.Common
{
    public class SequentialTasksExecutor
    {
        #region Attributes

        Task m_CurrentTask;

        #endregion

        #region Public methods

        public Task AddTask(Action _ToDo, TaskContinuationOptions _TaskContinuationOptions = TaskContinuationOptions.None)
        {
            Task actionTask;

            if (m_CurrentTask == null)
            {
                m_CurrentTask = (actionTask = Task.Factory.StartNew(() => _ToDo?.Invoke(), (TaskCreationOptions)_TaskContinuationOptions)).
                    ContinueWith(_Task => _Task.Dispose(), TaskContinuationOptions.ExecuteSynchronously);
            }
            else
            {
                m_CurrentTask = actionTask = m_CurrentTask.ContinueWith(_PreviousTask =>
                {
                    _PreviousTask?.Dispose();

                    _ToDo?.Invoke();
                },
                _TaskContinuationOptions);
            }

            return actionTask;
        }

        public Task<TResult> AddTask<TResult>(Func<TResult> _ToDo, TaskContinuationOptions _TaskContinuationOptions = TaskContinuationOptions.None)
        {
            Task<TResult> actionTask;

            if (m_CurrentTask == null)
            {
                m_CurrentTask = (actionTask = Task.Factory.StartNew(
                    () => _ToDo.Invoke(),
                    (TaskCreationOptions)_TaskContinuationOptions)).
                    ContinueWith(
                    _Task => _Task.Dispose(),
                    TaskContinuationOptions.ExecuteSynchronously);
            }
            else
            {
                m_CurrentTask = actionTask = m_CurrentTask.ContinueWith(_Task =>
                {
                    _Task?.Dispose();

                    return _ToDo.Invoke();
                }, _TaskContinuationOptions);
            }

            return actionTask;
        }

        #endregion
    }
}