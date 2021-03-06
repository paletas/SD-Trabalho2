﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading;

namespace CentralServiceProject
{
    [ServiceBehavior(IncludeExceptionDetailInFaults = true, ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single)]
    class CentralService : ICentralService
    {
        private long _currentId;
       
        private readonly ThemesContainer _container;

        public CentralService()
        {
            _container = new ThemesContainer(new[] { 
                new Theme("Sports", "Football, Tennis, ..."), 
                new Theme("Music", "Rock, Pop, ..."), 
                new Theme("Books", "Romance, Adventure, Fantasy, ...") 
            });
        }

        #region Implementation of ICentralService

        public Theme[] GetThemes()
        {
            return _container.GetThemes().ToArray();
        }

        public User[] LogOn(string themeName, string userName, Uri address)
        {
            Console.WriteLine("LogOn Enter");

            User self;
            User[] temp;
            Theme theme;
            lock(_container)
            {
                if ((self = _container.GetUser(userName)) == null)
                    self = new User(GenerateUniqueId(), userName, address);

                theme = _container.GetTheme(themeName);

                self.Callback = OperationContext.Current.GetCallbackChannel<IUserCallback>();
                try
                {
                    _container.AddUser(theme, self);
                }
                catch (InvalidOperationException e)
                {
                    throw new FaultException<InvalidOperationException>(e);
                }
                temp = new[] { self }.Concat(_container.GetUsers(theme).Where(u => !self.Equals(u))).ToArray();
            }

            Console.WriteLine("Username {0} got {1} new users.", userName, temp.Length); 
            
            Thread t = new Thread(() =>
                                      {
                                        LinkedList<User> toRemove = new LinkedList<User>();
                                        foreach(User user in _container.GetUsers(theme))
                                        {
                                            try
                                            {
                                                if (!user.Equals(self))
                                                {
                                                    if(((ICommunicationObject)user.Callback).State == CommunicationState.Opened)
                                                        user.Callback.OnUserJoined(self);
                                                    else
                                                        toRemove.AddLast(user);
                                                }
                                            }
                                            catch(TimeoutException e)
                                            {
                                                Console.WriteLine(e.Message);
                                                toRemove.AddLast(user);
                                            }
                                            catch(CommunicationException e)
                                            {
                                                Console.WriteLine(e.Message);
                                                toRemove.AddLast(user);
                                            }
                                            catch(ObjectDisposedException e)
                                            {
                                                Console.WriteLine(e.Message);
                                                toRemove.AddLast(user);
                                            }
                                        }

                                        foreach (User user in toRemove)
                                        {
                                            LogOff(theme.Name, user.Id);
                                        }
                                      });

            t.Start();

            Console.WriteLine("LogOn Exit");

            return temp;
        }

        public void LogOff(string themeName, long id)
        {
            Console.WriteLine("LogOff Enter");

            User user;
            Theme theme;
            lock (_container)
            {
                try
                {
                    user = _container.GetUser(id);
                }
                catch (InvalidOperationException e)
                {
                    throw new FaultException<InvalidOperationException>(e);
                }

                Console.WriteLine("User {0}", user.Name);

                theme = _container.GetTheme(themeName);
                _container.RemoveUser(theme, user);
            }

            Thread t = new Thread(() =>
            {
                LinkedList<User> toRemove = new LinkedList<User>();
                foreach (User user1 in _container.GetUsers(theme))
                {
                    try
                    {
                        if (((ICommunicationObject)user.Callback).State == CommunicationState.Opened)
                            user1.Callback.OnUserLeft(user);
                        else
                            toRemove.AddLast(user1);
                    }
                    catch (TimeoutException e)
                    {
                        Console.WriteLine(e.Message);
                        toRemove.AddLast(user1);
                    }    
                    catch (CommunicationException e)
                    {
                        Console.WriteLine(e.Message);
                        toRemove.AddLast(user1);
                    }
                    catch (ObjectDisposedException e)
                    {
                        Console.WriteLine(e.Message);
                        toRemove.AddLast(user1);
                    }
                }

                foreach(User user1 in toRemove)
                {
                    LogOff(theme.Name, user1.Id);
                }
            });

            t.Start();

            Console.WriteLine("LogOff Exit");
        }

        #endregion

        private long GenerateUniqueId()
        {
            return _currentId++;
        }

        public static void Main(string[] args)
        {
            using (ServiceHost serviceHost = new ServiceHost(typeof(CentralService)))
            {
                // Open the ServiceHost to create listeners and start listening for messages.
                serviceHost.Open();
                Console.WriteLine("[SERVICE] Service is starting!");

                Console.ReadLine();

                Console.WriteLine("[SERVICE] Service is ending!");
            }
        }
    }
}
