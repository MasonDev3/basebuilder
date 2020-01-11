using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JobSpriteController : MonoBehaviour
{

    // This bare-bones controller is mostly going to 
    // piggy-back on FurnitureSpriteController
    // because we don't yet fully know what our
    // job system will look like at the end
    // Start is called before the first frame update

    FurnitureSpriteController fsc;
    Dictionary<Job, GameObject> jobGameObjectMap;

    void Start()
    {
        fsc = FindObjectOfType<FurnitureSpriteController>();
        jobGameObjectMap = new Dictionary<Job, GameObject>();
        WorldController.instance.world.jobQueue.RegisterJobCreationCallback(OnJobCreated);
    }

    void OnJobCreated(Job job)
    {
        if (job.jobObjectType == null)
        {
            // This job doesn't really have an associated sprite with it
            // so no need to render
            return;
        }
        // We can only do furniture-building jobs
        // FIXME

        
        // Add to the dictionary

        if (jobGameObjectMap.ContainsKey(job))
        {
            //Debug.LogError("OnJobCreated for a job_gameObject that already exists -- most likely a job being REQUEUED as opposed to created.");
            return;
        }

        // Create a visual GameObject linked to this data
        GameObject job_gameObject = new GameObject();

        jobGameObjectMap.Add(job, job_gameObject);

        job_gameObject.name = "JOB_" + job.jobObjectType + "_" + job.tile.X + "_" + job.tile.Y;
        job_gameObject.transform.position = new Vector3(job.tile.X + ((job.furniturePrototype.Width - 1) / 2f), job.tile.Y + ((job.furniturePrototype.Height - 1) / 2f), 0);
        job_gameObject.transform.SetParent(this.transform, true);


        // Add SpriteRenderer but don't set a sprite.
        // FIXME: Wall sprite is currently assumed but should not be
        SpriteRenderer sr = job_gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = fsc.GetSpriteForFurniture(job.jobObjectType);
        sr.color = new Color(0.5f, 1.0f, 0.5f, 0.25f);
        sr.sortingLayerName = "Jobs";

        // FIXME: HARDCODING
        if (job.jobObjectType == "Door")
        {
            // by default, door graphic is meant for wall to be east and west
            // check to see if walls are north south
            // if so, rotate by 90 degrees
            Tile southTile = job.tile.world.GetTileAt(job.tile.X, job.tile.Y - 1);
            Tile northTile = job.tile.world.GetTileAt(job.tile.X, job.tile.Y + 1);

            if (northTile != null && southTile != null && northTile.furniture != null && southTile.furniture != null &&
                northTile.furniture.ObjectType == "Wall" && southTile.furniture.ObjectType == "Wall")
            {
                job_gameObject.transform.rotation = Quaternion.Euler(0, 0, 90);
            }
        }

        job.RegisterJobCompleteCallback(OnJobEnded);
        job.RegisterJobCancelCallback(OnJobEnded);
    }

    void OnJobEnded(Job j)
    {
        // This executes weather completed or cancelled
        GameObject job_gameObject = jobGameObjectMap[j];
        j.UnregisterJobCompleteCallback(OnJobEnded);
        j.UnregisterJobCancelCallback(OnJobEnded);

        Destroy(job_gameObject);
    }
}
