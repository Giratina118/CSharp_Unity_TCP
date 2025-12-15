using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Monster
{
    private int _id; // 몬스터 고유 번호
    private int _maxHP;
    private int _curHP;
    private float _moveSpeed;
    private int _damage;

    public Monster()
    {

    }

    public void Hit(int dmg)
    {
        _curHP -= dmg;
        if (_curHP <= 0)
            Die();
    }

    private void Die()
    {

    }

    public void Update()
    {

    }

    public void Move()
    {

    }

    public void Attack()
    {

    }
}

public class MonsterManager
{
    public static MonsterManager Instance { get; } = new MonsterManager();

    Dictionary<int, Monster> _monsters = new Dictionary<int, Monster>();
    private int _monsterId = 0;

    public void SpawnStart()
    {


    }
}