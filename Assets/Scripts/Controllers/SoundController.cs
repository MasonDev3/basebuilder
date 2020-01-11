using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundController : MonoBehaviour
{
    float soundCooldown = 0;
    // Start is called before the first frame update
    void Start()
    {
        WorldController.instance.world.RegisterFurnitureCreated(OnFurnitureCreated);
        WorldController.instance.world.RegisterTileChanged(OnTileTypeChanged);

    }

    // Update is called once per frame
    void Update()
    {
        soundCooldown -= Time.deltaTime;
    }

    public void OnTileTypeChanged(Tile tile_data)
    {
        if (soundCooldown > 0)
        {
            return;
        }
        AudioClip ac = Resources.Load<AudioClip>("Sounds/Floor_OnCreated");
        AudioSource.PlayClipAtPoint(ac, Camera.main.transform.position);
        soundCooldown = 0.1f;
    }

    public void OnFurnitureCreated(Furniture furn)
    {
        if (soundCooldown > 0)
        {
            return;
        }
        AudioClip ac = Resources.Load<AudioClip>("Sounds/" + furn.ObjectType + "_OnCreated");
        if (ac == null)
        {
            // WTF? WHAT DO
            // Since no specific sound.. use default (Wall_OnCreated) sound
            ac = Resources.Load<AudioClip>("Sounds/Wall_OnCreated");
        }
        AudioSource.PlayClipAtPoint(ac, Camera.main.transform.position);
        soundCooldown = 0.1f;
    }
}
