using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class JobQueue
{
    Queue<Job> jobQueue;
    Action<Job> cbJobCreated;

    public JobQueue()
    {
        jobQueue = new Queue<Job>();
    }

    public void Enqueue(Job j)
    {
        //Debug.Log("Adding job to queue. Existing queue size: " 
        //         + jobQueue.Count);
        if (j.jobTime < 0)
        {
            // Job has a negative job time, AKA its not supposed to be queued up
            // so insta-complete it
            // instead of Enqueueing
            j.DoWork(0);
            return;
        }
        jobQueue.Enqueue(j);
        
        cbJobCreated?.Invoke(j);
    }

    public Job Dequeue()
    {
        if (jobQueue.Count == 0)
        {
            return null;
        }
        return jobQueue.Dequeue();
    }

    public void Remove(Job j)
    {
        //TODO: Check docs to see if there's a less memory swamping solution
        List<Job> jobs = new List<Job>(jobQueue);
        if (jobs.Contains(j) == false)
        {
            // Most likely this job wasn't on the queue because a character was working it
            //Debug.LogError("Trying to remove job that doesn't exist on queue");
            return;
        }
        jobs.Remove(j);
        jobQueue = new Queue<Job>(jobs);
    }

    public void RegisterJobCreationCallback(Action<Job> callback)
    {
        cbJobCreated += callback;
    }

    public void UnregisterJobCreationCallback(Action<Job> callback)
    {
        cbJobCreated -= callback;
    }
}
