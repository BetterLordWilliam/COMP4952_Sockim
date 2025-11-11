create database sockimdatabase;
create user 'sockim_service'@'localhost' identified by '1234';
grant all privileges on sockimdatabase.* to 'sockim_service'@'localhost';
