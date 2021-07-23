%files
%config %attr(644, root, root) %{_bindir}/*

%files api
%defattr(-, onlyoffice, onlyoffice, -)
%{buildpath}/studio/api/
%{buildpath}/products/ASC.People/server/ASC.People.dll
%{buildpath}/products/ASC.Files/server/ASC.Files*.dll
%{buildpath}/products/ASC.CRM/server/ASC.CRM*.dll
%{buildpath}/products/ASC.Projects/server/ASC.Projects*.dll
lib/systemd/system/%{product}-api.service
%dir %{buildpath}/studio/
%dir %{buildpath}/products/ASC.People/
%dir %{buildpath}/products/ASC.People/server/
%dir %{buildpath}/products/ASC.Files/
%dir %{buildpath}/products/ASC.Files/server/
%dir %{buildpath}/products/ASC.CRM/
%dir %{buildpath}/products/ASC.CRM/server/
%dir %{buildpath}/products/ASC.Projects/
%dir %{buildpath}/products/ASC.Projects/server/

%files backup
%defattr(-, onlyoffice, onlyoffice, -)
%{buildpath}/services/ASC.Data.Backup/
%{buildpath}/products/ASC.People/server/ASC.People.dll
%{buildpath}/products/ASC.Files/server/ASC.Files*.dll
%{buildpath}/products/ASC.CRM/server/ASC.CRM*.dll
%{buildpath}/products/ASC.Projects/server/ASC.Projects*.dll
lib/systemd/system/%{product}-backup.service
%dir %{buildpath}/services/
%dir %{buildpath}/products/
%dir %{buildpath}/products/ASC.People/
%dir %{buildpath}/products/ASC.People/server/
%dir %{buildpath}/products/ASC.Files/
%dir %{buildpath}/products/ASC.Files/server/
%dir %{buildpath}/products/ASC.CRM/
%dir %{buildpath}/products/ASC.CRM/server/
%dir %{buildpath}/products/ASC.Projects/
%dir %{buildpath}/products/ASC.Projects/server/

%files common
%defattr(-, onlyoffice, onlyoffice, -)
%config %{_sysconfdir}/onlyoffice/%{product}/
%{_var}/log/onlyoffice/%{product}/
%{buildpath}/sql/
%dir %{_sysconfdir}/onlyoffice/
%dir %{_var}/log/onlyoffice/

%files files-services
%defattr(-, onlyoffice, onlyoffice, -)
%{buildpath}/products/ASC.Files/service/
%{buildpath}/products/ASC.Files/server/ASC.Files*.dll
%{buildpath}/products/ASC.People/server/ASC.People*.dll
%{buildpath}/products/ASC.CRM/server/ASC.CRM*.dll
%{buildpath}/products/ASC.Projects/server/ASC.Projects*.dll
lib/systemd/system/%{product}-files-services.service
%dir %{buildpath}/products/
%dir %{buildpath}/products/ASC.People/
%dir %{buildpath}/products/ASC.People/server
%dir %{buildpath}/products/ASC.Files/
%dir %{buildpath}/products/ASC.Files/server/
%dir %{buildpath}/products/ASC.CRM/
%dir %{buildpath}/products/ASC.CRM/server/
%dir %{buildpath}/products/ASC.Projects/
%dir %{buildpath}/products/ASC.Projects/server/

%files notify
%defattr(-, onlyoffice, onlyoffice, -)
%{buildpath}/services/ASC.Notify/
%{buildpath}/products/ASC.People/server/ASC.People.dll
%{buildpath}/products/ASC.Files/server/ASC.Files*.dll
%{buildpath}/products/ASC.CRM/server/ASC.CRM*.dll
%{buildpath}/products/ASC.Projects/server/ASC.Projects*.dll
lib/systemd/system/%{product}-notify.service
%dir %{buildpath}/services/
%dir %{buildpath}/products/
%dir %{buildpath}/products/ASC.People/
%dir %{buildpath}/products/ASC.People/server/
%dir %{buildpath}/products/ASC.Files/
%dir %{buildpath}/products/ASC.Files/server/
%dir %{buildpath}/products/ASC.CRM/
%dir %{buildpath}/products/ASC.CRM/server/
%dir %{buildpath}/products/ASC.Projects/
%dir %{buildpath}/products/ASC.Projects/server/

%files files
%defattr(-, onlyoffice, onlyoffice, -)
%{buildpath}/products/ASC.Files/server/
%{buildpath}/products/ASC.People/server/ASC.People.dll
%{buildpath}/products/ASC.CRM/server/ASC.CRM*.dll
%{buildpath}/products/ASC.Projects/server/ASC.Projects*.dll
lib/systemd/system/%{product}-files.service
%dir %{buildpath}/products/
%dir %{buildpath}/products/ASC.Files/
%dir %{buildpath}/products/ASC.People/
%dir %{buildpath}/products/ASC.People/server/
%dir %{buildpath}/products/ASC.CRM/
%dir %{buildpath}/products/ASC.CRM/server/
%dir %{buildpath}/products/ASC.Projects/
%dir %{buildpath}/products/ASC.Projects/server/

%files api-system
%defattr(-, onlyoffice, onlyoffice, -)
%{buildpath}/services/ASC.ApiSystem/
lib/systemd/system/%{product}-api-system.service
%dir %{buildpath}/services/

%files proxy
%defattr(-, onlyoffice, onlyoffice, -)
%{_sysconfdir}/nginx/includes/*
%{_sysconfdir}/nginx/conf.d/*
%{buildpath}/public/
%{buildpath}/studio/client/
%{buildpath}/studio/login
%{buildpath}/products/ASC.People/client/
%{buildpath}/products/ASC.Files/client/
%{buildpath}/products/ASC.Files/editor/
%{buildpath}/products/ASC.CRM/client/
%{buildpath}/products/ASC.Projects/client
%{buildpath}/products/ASC.Calendar/client/
%{buildpath}/products/ASC.Mail/client
%dir %{buildpath}/studio/
%dir %{buildpath}/products/
%dir %{buildpath}/products/ASC.People/
%dir %{buildpath}/products/ASC.Files/
%dir %{buildpath}/products/ASC.CRM/
%dir %{buildpath}/products/ASC.Projects/
%dir %{buildpath}/products/ASC.Calendar/
%dir %{buildpath}/products/ASC.Mail/

%files studio-notify
%defattr(-, onlyoffice, onlyoffice, -)
%{buildpath}/services/ASC.Studio.Notify/
%{buildpath}/products/ASC.People/server/ASC.People.dll
%{buildpath}/products/ASC.Files/server/ASC.Files*.dll
%{buildpath}/products/ASC.CRM/server/ASC.CRM*.dll
%{buildpath}/products/ASC.Projects/server/ASC.Projects*.dll
lib/systemd/system/%{product}-studio-notify.service
%dir %{buildpath}/services/
%dir %{buildpath}/products/
%dir %{buildpath}/products/ASC.People/
%dir %{buildpath}/products/ASC.People/server/
%dir %{buildpath}/products/ASC.Files/
%dir %{buildpath}/products/ASC.Files/server/
%dir %{buildpath}/products/ASC.CRM/
%dir %{buildpath}/products/ASC.CRM/server/
%dir %{buildpath}/products/ASC.Projects/
%dir %{buildpath}/products/ASC.Projects/server/

%files people-server
%defattr(-, onlyoffice, onlyoffice, -)
%{buildpath}/products/ASC.People/server/
%{buildpath}/products/ASC.Files/server/ASC.Files*.dll
%{buildpath}/products/ASC.CRM/server/ASC.CRM*.dll
%{buildpath}/products/ASC.Projects/server/ASC.Projects*.dll
lib/systemd/system/%{product}-people-server.service
%dir %{buildpath}/products/
%dir %{buildpath}/products/ASC.People/
%dir %{buildpath}/products/ASC.Files/
%dir %{buildpath}/products/ASC.Files/server/
%dir %{buildpath}/products/ASC.CRM/
%dir %{buildpath}/products/ASC.CRM/server/
%dir %{buildpath}/products/ASC.Projects/
%dir %{buildpath}/products/ASC.Projects/server/

%files urlshortener
%defattr(-, onlyoffice, onlyoffice, -)
%{buildpath}/services/ASC.UrlShortener/
%{buildpath}/services/ASC.UrlShortener.Svc/
lib/systemd/system/%{product}-urlshortener.service
%dir %{buildpath}/services/

%files thumbnails
%defattr(-, onlyoffice, onlyoffice, -)
%{buildpath}/services/ASC.Thumbnails/
%{buildpath}/services/ASC.Thumbnails.Svc/
lib/systemd/system/%{product}-thumbnails.service
%dir %{buildpath}/services/

%files socket
%defattr(-, onlyoffice, onlyoffice, -)
%{buildpath}/services/ASC.Socket.IO/
%{buildpath}/services/ASC.Socket.IO.Svc/
%{buildpath}/products/ASC.Files/server/
%{buildpath}/products/ASC.People/server/
%{buildpath}/products/ASC.CRM/server/
%{buildpath}/products/ASC.Projects/server/
lib/systemd/system/%{product}-socket.service
%dir %{buildpath}/services/
%dir %{buildpath}/products/
%dir %{buildpath}/products/ASC.Files/
%dir %{buildpath}/products/ASC.People/
%dir %{buildpath}/products/ASC.CRM/
%dir %{buildpath}/products/ASC.Projects/

%files studio
%defattr(-, onlyoffice, onlyoffice, -)
%{buildpath}/studio/server/
%{buildpath}/products/ASC.People/server/ASC.People.dll
%{buildpath}/products/ASC.Files/server/ASC.Files*.dll
%{buildpath}/products/ASC.CRM/server/ASC.CRM*.dll
%{buildpath}/products/ASC.Projects/server/ASC.Projects*.dll
lib/systemd/system/%{product}-studio.service
%dir %{buildpath}/studio/
%dir %{buildpath}/products/
%dir %{buildpath}/products/ASC.People/
%dir %{buildpath}/products/ASC.People/server/
%dir %{buildpath}/products/ASC.Files/
%dir %{buildpath}/products/ASC.Files/server/
%dir %{buildpath}/products/ASC.CRM/
%dir %{buildpath}/products/ASC.CRM/server/
%dir %{buildpath}/products/ASC.Projects/
%dir %{buildpath}/products/ASC.Projects/server/

%files storage-encryption
%defattr(-, onlyoffice, onlyoffice, -)
%{buildpath}/services/ASC.Data.Storage.Encryption/
%{buildpath}/products/ASC.Files/server/ASC.Files*.dll
%{buildpath}/products/ASC.People/server/ASC.People.dll
%{buildpath}/products/ASC.CRM/server/ASC.CRM*.dll
%{buildpath}/products/ASC.Projects/server/ASC.Projects*.dll
lib/systemd/system/%{product}-storage-encryption.service
%dir %{buildpath}/services/
%dir %{buildpath}/products/
%dir %{buildpath}/products/ASC.Files/
%dir %{buildpath}/products/ASC.Files/server
%dir %{buildpath}/products/ASC.People/
%dir %{buildpath}/products/ASC.People/server
%dir %{buildpath}/products/ASC.CRM/
%dir %{buildpath}/products/ASC.CRM/server
%dir %{buildpath}/products/ASC.Projects/
%dir %{buildpath}/products/ASC.Projects/server

%files storage-migration
%defattr(-, onlyoffice, onlyoffice, -)
%{buildpath}/services/ASC.Data.Storage.Migration/
%{buildpath}/products/ASC.Files/server/
%{buildpath}/products/ASC.People/server/
%{buildpath}/products/ASC.CRM/server/
%{buildpath}/products/ASC.Projects/server/
lib/systemd/system/%{product}-storage-migration.service
%dir %{buildpath}/services/
%dir %{buildpath}/products/
%dir %{buildpath}/products/ASC.Files/
%dir %{buildpath}/products/ASC.People/
%dir %{buildpath}/products/ASC.CRM/
%dir %{buildpath}/products/ASC.Projects/

%files projects-server
%defattr(-, onlyoffice, onlyoffice, -)
%{buildpath}/products/ASC.Projects/server/
%{buildpath}/products/ASC.Files/server/ASC.Files*.dll 
%{buildpath}/products/ASC.People/server/ASC.People.dll
%{buildpath}/products/ASC.CRM/server/ASC.CRM*.dll
lib/systemd/system/%{product}-projects-server.service
%dir %{buildpath}/products/
%dir %{buildpath}/products/ASC.Files/
%dir %{buildpath}/products/ASC.Files/server/
%dir %{buildpath}/products/ASC.People/
%dir %{buildpath}/products/ASC.People/server/
%dir %{buildpath}/products/ASC.CRM/
%dir %{buildpath}/products/ASC.CRM/server/
%dir %{buildpath}/products/ASC.Projects/

%files telegram-service
%defattr(-, onlyoffice, onlyoffice, -)
%{buildpath}/services/ASC.TelegramService/
%{buildpath}/products/ASC.Files/server/ASC.Files*.dll
%{buildpath}/products/ASC.People/server/ASC.People.dll
%{buildpath}/products/ASC.CRM/server/ASC.CRM*.dll
%{buildpath}/products/ASC.Projects/server/ASC.Projects*.dll
lib/systemd/system/%{product}-telegram-service.service
%dir %{buildpath}/services/
%dir %{buildpath}/products/
%dir %{buildpath}/products/ASC.Files/
%dir %{buildpath}/products/ASC.Files/server/
%dir %{buildpath}/products/ASC.People/
%dir %{buildpath}/products/ASC.People/server/
%dir %{buildpath}/products/ASC.CRM/
%dir %{buildpath}/products/ASC.CRM/server/
%dir %{buildpath}/products/ASC.Projects/
%dir %{buildpath}/products/ASC.Projects/server/

%files crm
%defattr(-, onlyoffice, onlyoffice, -)
%{buildpath}/products/ASC.CRM/server/
%{buildpath}/products/ASC.Files/server/ASC.Files*.dll
%{buildpath}/products/ASC.People/server/ASC.People.dll
%{buildpath}/products/ASC.Projects/server/ASC.Projects*.dll
lib/systemd/system/%{product}-crm.service
%dir %{buildpath}/products/
%dir %{buildpath}/products/ASC.CRM/
%dir %{buildpath}/products/ASC.Files/
%dir %{buildpath}/products/ASC.Files/server/
%dir %{buildpath}/products/ASC.Projects/
%dir %{buildpath}/products/ASC.Projects/server/

%files calendar
%defattr(-, onlyoffice, onlyoffice, -)
%{buildpath}/products/ASC.Calendar/server/
lib/systemd/system/%{product}-calendar.service
%dir %{buildpath}/products/
%dir %{buildpath}/products/ASC.Calendar/

%files mail
%defattr(-, onlyoffice, onlyoffice, -)
%{buildpath}/products/ASC.Mail/server/
lib/systemd/system/%{product}-mail.service
%dir %{buildpath}/products/
%dir %{buildpath}/products/ASC.Mail/
