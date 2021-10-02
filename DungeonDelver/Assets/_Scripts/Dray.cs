﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dray : MonoBehaviour, IFacingMover, IKeyMaster
{
    public enum eMode { idle, move, attack, transition, knockback }

    [Header("Set in Inspector")]
    public float speed = 6;
    public float attackDuration = 0.25f; //Продожительность атаки в секундах 
    public float attackDelay = 0.5f; //Задержка между атаками
    public float transitionDelay = 0.5f; //Задержка перехода между комнатами
    public int maxHealth = 10;
    public float knockbackSpeed = 10;
    public float knockbackDuration = 0.25f;
    public float invincibleDuration = 0.5f;

    [Header("Set Dynamically")]
    public int dirHeld = -1; //Направление
    public int facing = 1; //Направление движения Дрея
    public eMode mode = eMode.idle;
    public int numKeys = 0;
    public bool invincible = false;
    public bool hasGrappler = false;
    public Vector3 lastSafeLoc;
    public int lastSafeFacing;

    [SerializeField]
    private int _health;

    public int health
    {
        get { return _health; }
        set { _health = value; }
    }

    private float timeAtkDone = 0; //Время заверщения атаки
    private float timeAtkNext = 0; //Время, когда Дрей сможет повторить атаку

    private float transitionDone = 0;
    private Vector2 transitionPos;
    private float knockbackDone = 0;
    private float invincibleDone = 0;
    private Vector3 knockbackVel;

    private SpriteRenderer sRend;
    private Rigidbody rigid;
    private Animator anim;
    private InRoom inRm;

    private Vector3[] directions = new Vector3[]
    {
        Vector3.right, Vector3.up, Vector3.left, Vector3.down
    };

    private KeyCode[] keys = new KeyCode[]
    {
        KeyCode.RightArrow, KeyCode.UpArrow, KeyCode.LeftArrow, KeyCode.DownArrow
    };

    void Awake()
    {
        sRend = GetComponent<SpriteRenderer>();
        rigid = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        inRm = GetComponent<InRoom>();
        health = maxHealth;
        lastSafeLoc = transform.position; //Начальная позиция безопасна
        lastSafeFacing = facing;
    }

    void Update()
    {
        //Проверить состояние неуязвимости и необходимость выполнить отбрасывание
        if (invincible && Time.time > invincibleDone) invincible = false;
        sRend.color = invincible ? Color.red : Color.white;
        if(mode == eMode.knockback)
        {
            rigid.velocity = knockbackVel;
            if (Time.time < knockbackDone) return;
        }
        //Задержка перед переходом в другую комнату
        if(mode == eMode.transition)
        {
            rigid.velocity = Vector3.zero;
            anim.speed = 0;
            roomPos = transitionPos; //Оставить Дрея на месте
            if (Time.time < transitionDone) return;

            mode = eMode.idle;
        }


        dirHeld = -1;
        //Изменение движения по нажатиям клавиши
        for (int i = 0; i < 4; i++)
        {
            if (Input.GetKey(keys[i])) dirHeld = i;
        }

        //Нажата клавиша атаки
        if (Input.GetKeyDown(KeyCode.Space) && Time.time >= timeAtkNext)
        {
            mode = eMode.attack;
            timeAtkDone = Time.time + attackDuration;
            timeAtkNext = Time.time + attackDelay;
        }

        //Завершить атаку, если время истекло
        if (Time.time >= timeAtkDone)
        {
            mode = eMode.idle;
        }

        //Выбрать правильный режим, если Дрей не атакует
        if (mode != eMode.attack)
        {
            if (dirHeld == -1)
            {
                mode = eMode.idle;
            }
            else
            {
                facing = dirHeld;
                mode = eMode.move;
            }
        }


        //-----Действия в текущем режиме-----
        Vector3 vel = Vector3.zero;
        switch (mode)
        {
            case eMode.attack:
                anim.CrossFade("Dray_Attack_" + facing, 0);
                anim.speed = 0;
                break;

            case eMode.idle:
                anim.CrossFade("Dray_Walk_" + facing, 0);
                anim.speed = 0;
                break;

            case eMode.move:
                vel = directions[dirHeld];
                anim.CrossFade("Dray_Walk_" + facing, 0);
                anim.speed = 1;
                break;
        }

        rigid.velocity = vel * speed;

    }

    void LateUpdate()
    {
        //Получить координаты узла сетки, с размером ячейки
        // в половину единицы, ближайшего к данному персонажу
        Vector2 rPos = GetRoomPosOnGrid(0.5f);

        //Персонаж находится на плитке с дверью?
        int doorNum;
        for (doorNum = 0; doorNum < 4; doorNum++)
        {
            if (rPos == InRoom.DOORS[doorNum])
            {
                break;
            }
        }

        if (doorNum > 3 || doorNum != facing) return;

        //Перейти в следующию комнату
        Vector2 rm = roomNum;
        switch(doorNum)
        {
            case 0:
                rm.x += 1;
                break;
            case 1:
                rm.y += 1;
                break;
            case 2:
                rm.x -= 1;
                break;
            case 3:
                rm.y -= 1;
                break;
        }

        //Проверить, можно ли выполнить переход в комнату rm
        if(rm.x >= 0 && rm.x <= InRoom.MAX_RM_X)
        {
            if(rm.y >= 0 && rm.y <= InRoom.MAX_RM_X)
            {
                roomNum = rm;
                transitionPos = InRoom.DOORS[(doorNum + 2) % 4];
                roomPos = transitionPos;
                lastSafeLoc = transform.position;
                lastSafeFacing = facing;
                mode = eMode.transition;
                transitionDone = Time.time + transitionDelay;
            }
        }
    }

    void OnCollisionEnter(Collision coll)
    {
        if (invincible) return; //Выйти, если Дрей пока неуязвим
        DamageEffect dEf = coll.gameObject.GetComponent<DamageEffect>();
        if (dEf == null) return; //Если компонент DamageEffect отсутсвует - выйти

        health -= dEf.damage; //Вычесть величину ущерба из уровня здоровья 
        invincible = true; //Сделать неуязвимым
        invincibleDone = Time.time + invincibleDuration;

        if(dEf.knockback) // Полнить отбрасывание
        {
            //Определить направление отбрасывания
            Vector3 delta = transform.position - coll.transform.position;
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            {
                //Отбрасывание по горизонтали
                delta.x = (delta.x > 0) ? 1 : -1;
                delta.y = 0;
            }
            else
            {
                //Отбрасывание по вертикали
                delta.x = 0;
                delta.y = (delta.y > 0) ? 1 : -1;
            }

            //Применить скорость отскока к компоненту Ridigbody
            knockbackVel = delta * knockbackSpeed;
            rigid.velocity = knockbackVel;

            //Установить режим knockbakc и время прекращения отбрасывания
            mode = eMode.knockback;
            knockbackDone = Time.time + knockbackDuration;
        }
    }

    void OnTriggerEnter(Collider colld)
    {
        PickUp pup = colld.GetComponent<PickUp>();
        if (pup == null) return;

        switch (pup.itemType)
        {
            case PickUp.eType.health:
                health = Mathf.Min(health + 2, maxHealth);
                break;

            case PickUp.eType.key:
                keyCount++;
                break;

            case PickUp.eType.grappler:
                hasGrappler = true;
                break;
        }

        Destroy(colld.gameObject);
    }

    public void ResetInRoom(int healthLoss = 0)
    {
        transform.position = lastSafeLoc;
        facing = lastSafeFacing;
        health -= healthLoss;

        invincible = true;
        invincibleDone = Time.time + invincibleDuration;
    }


    //Реализация интерфейса IFacingMover
    public int GetFacing()
    {
        return facing;
    }

    public bool moving
    {
        get
        {
            return (mode == eMode.move);
        }
    }

    public float GetSpeed()
    {
        return speed;
    }

    public float gridMult
    {
        get { return inRm.gridMult; }
    }

    public Vector2 roomPos
    {
        get { return inRm.roomPos; }
        set { inRm.roomPos = value; }
    }

    public Vector2 roomNum
    {
        get { return inRm.roomNum; }
        set { inRm.roomNum = value; }
    }

    public Vector2 GetRoomPosOnGrid(float mult = -1)
    {
        return inRm.GetRoomPosOnGrid(mult);
    }

    //Реализация интерфейса IKeyMaster
    public int keyCount
    {
        get { return numKeys; }
        set { numKeys = value; }
    }
    
}