using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pathfinding;

public class ProceduralWorld : MonoBehaviour {
    public Transform target;

    public ProceduralPrefab[] prefabs;

    /// How far away to generate tiles
    public int range = 1;

    public int disableAsyncLoadWithinRange = 1;

    /// World size of tiles
    public float tileSize = 100;
    public int subTiles = 20;


    Queue<IEnumerator> tileGenerationQueue = new Queue<IEnumerator>();

    public enum RotationRandomness {
        AllAxes,
        Z
    }

    [System.Serializable]
    public class ProceduralPrefab {
        /// Prefab to use
        public GameObject prefab;

        /// Number of objects per square world unit
        public float density = 0;

        /// Multiply by [perlin noise].
        /// Value from 0 to 1 indicating weight.
        public float perlin = 0;

        /// Perlin will be raised to this power.
        /// A higher value gives more distinct edges
        public float perlinPower = 1;

        /// Some offset to avoid identical density maps
        public Vector2 perlinOffset = Vector2.zero;

        /// Perlin noise scale.
        /// A higher value spreads out the maximums and minimums of the density.
        public float perlinScale = 1;

        /// Multiply by [random].
        /// Value from 0 to 1 indicating weight.
        public float random = 1;

        public RotationRandomness randomRotation = RotationRandomness.AllAxes;

        /// If checked, a single object will be created in the center of each tile
        public bool singleFixed = false;
    }

    /// All tiles
    Dictionary<Int2, ProceduralTile> tiles = new Dictionary<Int2, ProceduralTile>();

    // Use this for initialization
    void Start () {
        // Calculate the closest tiles
        // and then recalculate the graph
        Update();
        AstarPath.active.Scan();

        StartCoroutine(GenerateTiles());
    }

    // Update is called once per frame
    void Update () {
        // Calculate the tile the target is standing on
        Int2 p = new Int2(Mathf.RoundToInt((target.position.x - tileSize*0.5f) / tileSize), Mathf.RoundToInt((target.position.y - tileSize*0.5f) / tileSize));

        // Clamp range
        range = range < 1 ? 1 : range;

        // Remove tiles which are out of range
        bool changed = true;
        while (changed) {
            changed = false;
            foreach (KeyValuePair<Int2, ProceduralTile> pair in tiles) {
                if (Mathf.Abs(pair.Key.x-p.x) > range || Mathf.Abs(pair.Key.y-p.y) > range) {
                    pair.Value.Destroy();
                    tiles.Remove(pair.Key);
                    changed = true;
                    break;
                }
            }
        }

        // Add tiles which have come in range
        // and start calculating them
        for (int x = p.x-range; x <= p.x+range; x++) {
            for (int y = p.y-range; y <= p.y+range; y++) {
                if (!tiles.ContainsKey(new Int2(x, y))) {
                    ProceduralTile tile = new ProceduralTile(this, x, y);
                    var generator = tile.Generate();
                    // Tick it one step forward
                    generator.MoveNext();
                    // Calculate the rest later
                    tileGenerationQueue.Enqueue(generator);
                    tiles.Add(new Int2(x, y), tile);
                }
            }
        }

        // The ones directly adjacent to the current one
        // should always be completely calculated
        // make sure they are
        for (int x = p.x-disableAsyncLoadWithinRange; x <= p.x+disableAsyncLoadWithinRange; x++) {
            for (int y = p.y-disableAsyncLoadWithinRange; y <= p.y+disableAsyncLoadWithinRange; y++) {
                tiles[new Int2(x, y)].ForceFinish();
            }
        }
    }

    IEnumerator GenerateTiles () {
        while (true) {
            if (tileGenerationQueue.Count > 0) {
                var generator = tileGenerationQueue.Dequeue();
                yield return StartCoroutine(generator);
            }
            yield return null;
        }
    }

    class ProceduralTile {
        int x, y;
        System.Random rnd;

        ProceduralWorld world;

        public bool destroyed { get; private set; }

        public ProceduralTile (ProceduralWorld world, int x, int y) {
            this.x = x;
            this.y = y;
            this.world = world;
            rnd = new System.Random((x * 10007) ^ (y*36007));
        }

        Transform root;
        IEnumerator ie;

        public IEnumerator Generate () {
            ie = InternalGenerate();
            GameObject rt = new GameObject("Tile " + x + " " + y);
            root = rt.transform;
            while (ie != null && root != null && ie.MoveNext()) yield return ie.Current;
            ie = null;
        }

        public void ForceFinish () {
            while (ie != null && root != null && ie.MoveNext()) {}
            ie = null;
        }

        Vector3 RandomInside () {
            Vector2 v = new Vector2();

            v.x = (x + (float)rnd.NextDouble())*world.tileSize;
            v.y = (y + (float)rnd.NextDouble())*world.tileSize;
            return v;
        }

        Vector3 RandomInside (float px, float py) {
            Vector2 v = new Vector2();

            v.x = (px + (float)rnd.NextDouble()/world.subTiles)*world.tileSize;
            v.y = (py + (float)rnd.NextDouble()/world.subTiles)*world.tileSize;
            return v;
        }

        Quaternion RandomZRot (ProceduralPrefab prefab) {
            return Quaternion.Euler(0, 0, 360 * (float)rnd.NextDouble());
        }

        IEnumerator InternalGenerate () {
            Debug.Log("Generating tile " + x + ", " + y);
            int counter = 0;

            float[, ] ditherMap = new float[world.subTiles+2, world.subTiles+2];

            //List<GameObject> objs = new List<GameObject>();

            for (int i = 0; i < world.prefabs.Length; i++) {
                ProceduralPrefab pref = world.prefabs[i];

                if (pref.singleFixed) {
                    Vector3 p = new Vector3((x+0.5f) * world.tileSize, (y+0.5f) * world.tileSize, 0);
                    GameObject ob = GameObject.Instantiate(pref.prefab, p, Quaternion.identity) as GameObject;
                    ob.transform.parent = root;
                } else {
                    float subSize = world.tileSize/world.subTiles;

                    for (int sx = 0; sx < world.subTiles; sx++) {
                        for (int sz = 0; sz < world.subTiles; sz++) {
                            ditherMap[sx+1, sz+1] = 0;
                        }
                    }

                    for (int sx = 0; sx < world.subTiles; sx++) {
                        for (int sy = 0; sy < world.subTiles; sy++) {
                            float px = x + sx/(float)world.subTiles;
                            float py = y + sy/(float)world.subTiles;

                            float perl = Mathf.Pow(Mathf.PerlinNoise((px + pref.perlinOffset.x)*pref.perlinScale, (py + pref.perlinOffset.y)*pref.perlinScale), pref.perlinPower);

                            float density = pref.density * Mathf.Lerp(1, perl, pref.perlin) * Mathf.Lerp(1, (float)rnd.NextDouble(), pref.random);
                            float fcount = subSize*subSize*density + ditherMap[sx+1, sy+1];
                            int count = Mathf.RoundToInt(fcount);

                            // Apply dithering - used to determine number of objects in the next sub-tile
                            // http://en.wikipedia.org/wiki/Floyd%E2%80%93Steinberg_dithering
                            ditherMap[sx+1+1, sy+1+0] += (7f/16f) * (fcount - count);
                            ditherMap[sx+1-1, sy+1+1] += (3f/16f) * (fcount - count);
                            ditherMap[sx+1+0, sy+1+1] += (5f/16f) * (fcount - count);
                            ditherMap[sx+1+1, sy+1+1] += (1f/16f) * (fcount - count);

                            // Create a number of objects
                            for (int j = 0; j < count; j++) {
                                // Find a random position inside the current sub-tile
                                Vector3 p = RandomInside(px, py);
                                GameObject ob = GameObject.Instantiate(pref.prefab, p, RandomZRot(pref)) as GameObject;
                                ob.transform.parent = root;
                                
                                //ob.SetActive ( false );
                                //objs.Add ( ob );
                                counter++;
                                if (counter % 2 == 0)
                                    yield return null;
                            }
                        }
                    }
                }
            }

            ditherMap = null;

            //waits two frames after all objects are generating before finishing coroutine
            //this lets all the objects complete their start/awake functions and lets a second
            //frame go by so if there are any other systems that use them they get a cycle to run
            //with the objects fully set up. 
            yield return null;
            yield return null;
        }

        public void Destroy () {
            if (root != null) {
                Debug.Log("Destroying tile " + x + ", " + y);
                GameObject.Destroy(root.gameObject);
                root = null;
            }

            // Make sure the tile generator coroutine is destroyed
            ie = null;
        }
    }
}

