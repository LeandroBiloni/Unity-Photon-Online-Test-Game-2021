﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEditor;

public class Character : MonoBehaviourPun, IPunObservable
{
    private PhotonView _myView;
    private string _playerName;
    public float maxHp;
    private float _hp;
    public Image hpBar;
    public Rigidbody rb;

    public float jumpForce;

    public float moveSpeed;

	Animator _anim;

    private float _horizontal;

    public List<Transform> spawnPoints = new List<Transform>();
    
    public float cooldown;
    
    private bool _jumping;
    private bool _grounded;
    private  bool _canShoot;
    private bool _canMove;

    public TextMeshPro nameText;

    private Player _owner;

    private Quaternion _originalRotation;
    
    // Start is called before the first frame update
    void Start()
    {
        _myView = GetComponent<PhotonView>();
		_anim = GetComponent<Animator>();

		if (!_myView.IsMine) return;
        _canShoot = true;
        _canMove = true;
        _hp = maxHp;
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            ResetRotation();
        }
    }

    public void Move(Vector3 dir)
    {
        rb.AddForce(dir.normalized * moveSpeed);

        var velocity = rb.velocity;
        if (dir.x == 0)
        {
            velocity.x = 0;
        }
        else if (velocity.x > 10)
        {
            velocity.x = moveSpeed;
        }
        else if (velocity.x < -10)
        {
            velocity.x = -moveSpeed;
        }
        
		//-------------------------------*********
		if (dir != Vector3.zero)
			_anim.SetBool("isMoving", true);
		else
			_anim.SetBool("isMoving", false);
    }
    
    public void Jump()
    {
        if (_jumping) return;
		//-------------------------------*********
		_anim.SetTrigger("Jump");

		_jumping = true;
        _grounded = false;
        rb.constraints = RigidbodyConstraints.None;
        rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationX |
                          RigidbodyConstraints.FreezeRotationY;
        rb.AddForce(transform.up * jumpForce);
    }

    public void Shoot(Vector3 mousePos)
    {
        if (!_canShoot) return;
		//-------------------------------*********
		_anim.SetTrigger("Attack");

		_canShoot = false;
        StartCoroutine(Cooldown());
        Vector3 closest = Vector3.zero;
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            if (i == 0)
            {
                closest = spawnPoints[i].position;
                continue;
            }
                
            if ((mousePos - spawnPoints[i].position).magnitude <
                (mousePos - closest).magnitude)
                closest = spawnPoints[i].position;

        }

        var dir = (mousePos - transform.position);
        dir.z = 0;
        
        PhotonNetwork.Instantiate("Projectile", closest, Quaternion.identity).GetComponent<Projectile>().SetOwner(this).SetDir(dir.normalized);
    }

    IEnumerator Cooldown()
    {
        yield return new WaitForSeconds(cooldown);
        _canShoot = true;
    }
    
    
    public void Push(Vector3 dir, float force, Vector3 collisionPoint)
    {
        rb.AddForceAtPosition(dir * force, collisionPoint);
    }
    
    
    public void Damage(int damage)
    {
		//-------------------------------*********
		_anim.SetTrigger("Hurt");

		_hp -= damage;

        if (!(_hp <= 0)) return;
        
        Server.Instance.PlayerLose(_owner);
    }
    
    void ResetRotation()
    {
        rb.constraints = RigidbodyConstraints.None;
        rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationX |
                          RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
        _jumping = false;
        _grounded = true;
        transform.rotation = _originalRotation;
        if (_horizontal == 0)
            rb.velocity = Vector3.zero;
    }
    public void UpdateLifeBar(float currentHP)
    {
        hpBar.fillAmount = currentHP / maxHp;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(_hp);
        }
        else
        {
            _hp = (float) stream.ReceiveNext();
        }
    }

    public float GetHP()
    {
        return _hp;
    }

    private void SetPlayerName(string name)
    {
        _playerName = name;
        if (_myView == null)
        {
            _myView = GetComponent<PhotonView>();
        }

        Server.Instance.SetPlayerName(_myView.ViewID, _playerName);
    }

    public void UpdateName(string name)
    {
        nameText.text = name;
    }
    
    public Character SetInitialParameters(Player localPlayer)
    {
        _owner = localPlayer;
        _originalRotation = transform.rotation;
        SetPlayerName(localPlayer.NickName);
        photonView.RPC("SetLocalParams", _owner, maxHp);
        return this;
    }

	public Player GetCharacterAsPlayer()
	{
		return _owner;
	} 

    [PunRPC]
    void SetLocalParams(float life)
    {
        _hp = life;
        nameText.color = Color.white;
    }
}
