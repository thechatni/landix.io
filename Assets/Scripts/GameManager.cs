using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SA
{
    public class GameManager : MonoBehaviour
    {
        public int maxHeight = 50;
        public int maxWIdth = 50;

        public Color color1;
        public Color color2;
        public Color playerColor = Color.grey;

        public Transform cameraHolder;

        GameObject playerObj;
        Node playerNode;
        Node prevPlayerNode;
        Sprite playerSprite;
        GameObject mapObject;
        SpriteRenderer mapRenderer;
        GameObject tailParent;
        Node[,] grid;
        int noSides = 0;
        List<int> xVertices = new List<int>();
        List<int> yVertices = new List<int>();
        List<SpecialNode> tail = new List<SpecialNode>();
        List<SpecialNode> reserve = new List<SpecialNode>();
        int minX=50, minY=50, maxX=0, maxY=0;
        bool cut = false;
        bool up, left, right, down;
        public float moveRate = 0.5f;
        public bool isGameOver;
        public bool isFirstInput;
        float timer;
        Direction curDirection;
        Direction targetDirection;
        public enum Direction
        {
            up,down,left,right
        }

        public UnityEvent onStart;
        public UnityEvent onGameOver;
        public UnityEvent firstInput;

        #region Init
        void Start()
        {
            onStart.Invoke();
        }
        
        void setBG()
        {
            Texture2D txt = new Texture2D(maxWIdth, maxHeight);
            for (int x = 0; x < maxWIdth; x++)
            {
                for (int y = 0; y < maxHeight; y++)
                {
                    if (x % 2 != 0)
                    {
                        if (y % 2 != 0)
                        {
                            txt.SetPixel(x, y, color1);
                        }
                        else
                        {
                            txt.SetPixel(x, y, color2);
                        }
                    }
                    else
                    {
                        if (y % 2 != 0)
                        {
                            txt.SetPixel(x, y, color2);
                        }
                        else
                        {
                            txt.SetPixel(x, y, color1);
                        }
                    }
                 
                }
            }

        }
        public void StartNewGame()
        {
            ClearReferences();
            CreateMap();
            PlacePlayer();
            PlaceCamera();
            setStartRange();
            isGameOver = false;
        }

        public void ClearReferences()
        {
            if(mapObject!=null)
                Destroy(mapObject);

            if(playerObj!=null)
                Destroy(playerObj);

            foreach (var t in tail)
            {
                if(t.obj!=null)
                    Destroy(t.obj);
            }

            foreach (var r in reserve)
            {
                if (r.obj != null)
                    Destroy(r.obj);
            }
            tail.Clear();
            reserve.Clear();
            xVertices.Clear();
            yVertices.Clear();
            grid = null;
            noSides = 0;
            targetDirection = Direction.up;
        }
        void CreateMap()
        {
            mapObject = new GameObject("Map");
            mapRenderer = mapObject.AddComponent<SpriteRenderer>();

            grid = new Node[maxWIdth, maxHeight];

            Texture2D txt = new Texture2D(maxWIdth, maxHeight);
            for (int x = 0; x < maxWIdth; x++)
            {
                for (int y = 0; y < maxHeight; y++)
                {
                    Vector3 tp = Vector3.zero;
                    tp.x = x;
                    tp.y = y;
                    Node n = new Node()
                    {
                        x = x,
                        y = y,
                        worldPosition = tp 
                    };

                    grid[x, y] = n;
                    #region visual
                    if (x%2!=0)
                    {
                        if(y%2 != 0)
                        {
                            txt.SetPixel(x, y, color1);
                        }
                        else
                        {
                            txt.SetPixel(x, y, color2);
                        }
                    }
                    else
                    {
                        if (y % 2 != 0)
                        {
                            txt.SetPixel(x, y, color2);
                        }
                        else
                        {
                            txt.SetPixel(x, y, color1);
                        }
                    }
                    #endregion
                }
            }
            
            txt.filterMode = FilterMode.Point;
            txt.Apply();
            Rect rect = new Rect(0, 0, maxWIdth, maxHeight);
            Sprite sprite = Sprite.Create(txt, rect, Vector2.zero, 1, 0, SpriteMeshType.FullRect);
            mapRenderer.sprite = sprite;
        }

        void PlacePlayer()
        {
            playerObj = new GameObject("Player");
            SpriteRenderer playerRender = playerObj.AddComponent<SpriteRenderer>();
            playerSprite = CreateSprite(playerColor);
            playerRender.sprite = playerSprite;
            playerRender.sortingOrder = 1;
            playerNode = GetNode(maxWIdth/2 -1, maxHeight/2 -1);
            playerObj.transform.position =playerNode.worldPosition;
            
            tailParent = new GameObject("tailParent");
            
        }
        #endregion

        void PlaceCamera()
        {
            Node n = GetNode(maxWIdth / 2, maxHeight / 2);
            Vector3 p = n.worldPosition;
            p += Vector3.one * .5f;
            cameraHolder.position = p;
        }
        #region Update
        
        private void Update()
        {
            if (isGameOver)
            {
                if (Input.GetKeyDown(KeyCode.R))
                {
                    onStart.Invoke();
                }
                return;
            }
                

            GetInput();
   
            SetPlayerDirection();
            timer += Time.deltaTime;
            if (timer > moveRate)
            {
                timer = 0;
                curDirection = targetDirection;
                MovePLayer();
                
            }
        }

        void setStartRange()
        {
            setBG();
            for (int i=0;i<4;i++)
            {
                for(int j = 0; j < 4; j++)
                {
                    reserve.Add(CreateReserveNode((maxWIdth-2)/2+i, (maxHeight - 2)/2 + j));
                }
            }
        }
        void GetInput()
        {
            up = Input.GetButtonDown("Up");
            down = Input.GetButtonDown("Down");
            left = Input.GetButtonDown("Left");
            right = Input.GetButtonDown("Right");
        }

        void SetPlayerDirection()
        {
            if (up)
            {
                if(!isOpposite(Direction.up))
                    targetDirection = Direction.up;
                
            }
            else if(down)
            {
                if (!isOpposite(Direction.down))
                    targetDirection = Direction.down;
                
            }
            else if (left)
            {
                if (!isOpposite(Direction.left))
                    targetDirection = Direction.left;
                
            }
            else if (right)
            {
                if (!isOpposite(Direction.right))
                    targetDirection = Direction.right;
                
            }
        }

        void MovePLayer()
        {
            int x = 0, y = 0;
             
            switch (curDirection)
            {
                case Direction.up:
                    y = 1;
                    /*if(playerNode.x<=minX)
                    {
                        minX = playerNode.x;
                    }
                    else if(playerNode.x>=maxX)
                    {
                        maxX = playerNode.x;
                    }*/
                    break;
                case Direction.down:
                    y = -1;
                    /*if (playerNode.x <= minX)
                    {
                        minX = playerNode.x;
                    }
                    else if (playerNode.x >= maxX)
                    {
                        maxX = playerNode.x;
                    }*/
                    break;
                case Direction.left:
                    x = -1;
                    /*if (playerNode.y <= minY)
                    {
                        minY = playerNode.y;
                    }
                    else if (playerNode.y >= maxY)
                    {
                        maxY = playerNode.y;
                    }*/
                    break;
                case Direction.right:
                    x = 1;
                    /*if (playerNode.y <= minY)
                    {
                        minY = playerNode.y;
                    }
                    else if (playerNode.y >= maxY)
                    {
                        maxY = playerNode.y;
                    }*/
                    break;
            }

            Node targetNode = GetNode(playerNode.x + x, playerNode.y + y);
            if (targetNode == null)
            {
                onGameOver.Invoke();
            }
            else
            {
                if (isTailNode(targetNode)==true) { 
                    if (isReserveNode(targetNode)==false)
                    {
                        onGameOver.Invoke();
                    }
                }
                else
                { 
                Node previousNode = playerNode;
                
                playerObj.transform.position = targetNode.worldPosition;
                playerNode = targetNode;
                tail.Add(CreateTailNode(previousNode.x, previousNode.y));
                xVertices.Add(previousNode.x);
                yVertices.Add(previousNode.y);
                noSides++;
                
                  if (isReserveNode(targetNode))
                    {
                        checkReserve();
                        xVertices.Add(previousNode.x);
                        yVertices.Add(previousNode.y);
                        noSides++;
                        tail.Clear();
                    }

                }
            }
        }

        void checkReserve()
        {

            foreach (var t in tail)
            {
                reserve.Add(t);
            }
            for (int r=0; r<maxHeight;r++)
            {
                for(int c=0; c<maxWIdth; c++)
                {
                    if (pointInPolygon(c,r)==true)
                    {
                        reserve.Add(CreateReserveNode(c,r));
                    }

                    

                }
            }
        }

        /*public bool IsPointInPolygon(Node p, List<Node> polygon)
        {
            double minX = polygon[0].x;
            double maxX = polygon[0].x;
            double minY = polygon[0].y;
            double maxY = polygon[0].y;
            for (int i = 1; i < polygon.Count; i++)
            {
                Node q = polygon[i];
                minX = System.Math.Min(q.x, minX);
                maxX = System.Math.Max(q.x, maxX);
                minY = System.Math.Min(q.x, minY);
                maxY = System.Math.Max(q.x, maxY);
            }

            if (p.x < minX || p.x > maxX || p.y < minY || p.y > maxY)
            {
                return false;
            }
            
            bool inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                if ((polygon[i].y > p.y) != (polygon[j].y > p.y) &&
                     p.x < (polygon[j].x - polygon[i].x) * (p.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }
        */
        bool pointInPolygon(int x, int y)
        {

            int i, j = noSides - 1;
            bool oddNodes = false;

            for (i = 0; i < noSides; i++)
            {
                if (yVertices[i] < y && yVertices[j] >= y
                || yVertices[j] < y && yVertices[i] >= y)
                {
                    if (xVertices[i] + (y - yVertices[i]) / (yVertices[j] - yVertices[i]) * (xVertices[j] - xVertices[i]) < x)
                    {
                        oddNodes = !oddNodes;
                    }
                }
                j = i;
            }

            return oddNodes;
        }
        #endregion

        #region Utilities

        public void GameOver()
        {
            isGameOver = true;
            isFirstInput = false;
        }
        bool isOpposite(Direction d)
        {
            switch (d)
            {
                default:
                case Direction.up:
                    if (curDirection == Direction.down)
                        return true;
                    else
                        return false;

                case Direction.down:
                    if (curDirection == Direction.up)
                        return true;
                    else
                        return false;

                case Direction.left:
                    if (curDirection == Direction.right)
                        return true;
                    else
                        return false;
                case Direction.right:
                    if (curDirection == Direction.left)
                        return true;
                    else
                        return false;

            }
        }

        bool isTailNode(Node n)
        {
            for (int i=0; i<tail.Count; i++)
            {
                if(tail[i].node == n)
                {
                    return true;
                }
            }

            return false;
        }

        bool isReserveNode(Node n)
        {
            for (int i = 0; i < reserve.Count; i++)
            {
                if (reserve[i].node == n)
                {
                    return true;
                }
            }

            return false;
        }

        Node GetNode(int x, int y)
        {
            if (x < 0 || x > maxWIdth - 1 || y < 0 || y > maxHeight - 1)
                return null;

            return grid[x, y];
        }

        SpecialNode CreateTailNode(int x, int y)
        {
            SpecialNode s = new SpecialNode();
            s.node = GetNode(x, y);
            s.obj = new GameObject();
            s.obj.transform.parent = tailParent.transform;
            s.obj.transform.position = s.node.worldPosition;
 
            SpriteRenderer r = s.obj.AddComponent<SpriteRenderer>();
            r.sprite = playerSprite;
            r.sortingOrder = 1;
            return s;
        }

        SpecialNode CreateReserveNode(int x, int y)
        {
            SpecialNode s = new SpecialNode();
            s.node = GetNode(x,y);
            s.obj = new GameObject();
            s.obj.transform.position = s.node.worldPosition;
            SpriteRenderer r = s.obj.AddComponent<SpriteRenderer>();
            r.sprite = playerSprite;
            r.sortingOrder = 1;
            return s;
        }

        Sprite CreateSprite(Color targetColor)
        {
            Texture2D txt = new Texture2D(1, 1);
            txt.SetPixel(0, 0, targetColor);
            txt.Apply();
            txt.filterMode = FilterMode.Point;
            Rect rect = new Rect(0, 0, 1, 1);
            return Sprite.Create(txt, rect, Vector2.zero, 1, 0, SpriteMeshType.FullRect,new Vector4(1,1,1,1));

        }
        #endregion
    }
}
