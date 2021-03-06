﻿using System;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace CentralServiceProject
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "ICentralService" in both code and config file together.
    [ServiceContract(CallbackContract = typeof(IUserCallback))]
    public interface ICentralService
    {
        //[OperationContract]
        //Theme CreateTheme(string description);

        [OperationContract]
        Theme[] GetThemes();

        [OperationContract]
        [FaultContract(typeof(InvalidOperationException))]
        User[] LogOn(string themeName, string userName, Uri address);

        [OperationContract]
        [FaultContract(typeof(InvalidOperationException))]
        void LogOff(string themeName, long id);
    }

    public interface IUserCallback
    {
        [OperationContract(IsOneWay = true)]
        void OnUserJoined(User user);

        [OperationContract(IsOneWay = true)]
        void OnUserLeft(User user);
    }

    // Use a data contract as illustrated in the sample below to add composite types to service operations.
    [DataContract]
    public class User
    {
        public User(){}

        public User(long id, string name, Uri chatService)
        {
            Id = id;
            Name = name;
            ChatService = chatService;
        }

        [DataMember]
        public long Id { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public Uri ChatService { get; set; }
    
        [DataMember]
        public IUserCallback Callback { get; set; }

        public override int GetHashCode()
        {
            return (int) Id;
        }

        public override bool Equals(object obj)
        {
            var otherUser = obj as User;
            return Equals(otherUser);
        }

        public bool Equals(User otherUser)
        {
            if (otherUser != null)
                return Id == otherUser.Id;
            return false;
        }
    }

    [DataContract]
    public class Theme
    {
        public Theme(string name, string description)
        {
            Name = name; 
            Description = description;
        }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var otherTheme = obj as Theme;
            if (otherTheme != null)
                return Name.Equals(otherTheme.Name);
            return false;
        }
    }

    [DataContract]
    public class UserFault
    {
        [DataMember]
        public String Reason { get; set; }
    }
}
