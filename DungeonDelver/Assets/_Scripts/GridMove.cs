using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridMove : MonoBehaviour
{
    private IFacingMover mover;

    void Awake()
    {
        mover = GetComponent<IFacingMover>();
    }

    void FixedUpdate()
    {
        if (!mover.moving) return;
        int facing = mover.GetFacing();

        //Применение выравнивания по сетке
        //Получить координаты близжайшего узла
        Vector2 rPos = mover.roomPos;
        Vector2 rPosGrid = mover.GetRoomPosOnGrid();

        //Подвинуть объект в сторону линии сетки
        float delta = 0;
        if (facing == 0 || facing == 2)
        {
            //Движение по горизонтали, выравнивание по оси Y
            delta = rPosGrid.y - rPos.y;
        }
        else
        {
            //Движение по вертикали, выравнивание по оси x
            delta = rPosGrid.x - rPos.x;
        }

        if (delta == 0) return; //Объект уже выровнен по сетке

        float move = mover.GetSpeed() * Time.fixedDeltaTime;
        move = Mathf.Min(move, Mathf.Abs(delta));
        if (delta < 0) move = -move;

        if (facing == 0 || facing == 2)
            rPos.y += move;
        else
            rPos.x += move;

        mover.roomPos = rPos;

    }
}