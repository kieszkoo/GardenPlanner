CREATE TABLE TypGleby (
    ID INT AUTO_INCREMENT PRIMARY KEY,
    Nazwa VARCHAR(100) NOT NULL,
    RetencjaWody DECIMAL(3, 2) NOT NULL,
    PoziomOdzywczy DECIMAL(3, 2) NOT NULL,
    Kwasowosc DECIMAL(3, 2) NOT NULL,
    Zyznosc DECIMAL(3, 2) NOT NULL
);

INSERT INTO TypGleby (Nazwa, RetencjaWody, PoziomOdzywczy, Kwasowosc, Zyznosc) VALUES
('Piaszczysta', 0.20, 0.20, 0.50, 0.20),
('Gliniasta', 0.60, 0.50, 0.60, 0.50),
('Lessowa', 0.80, 0.80, 0.50, 0.80),
('Torfowa', 0.90, 0.40, 0.35, 0.40),
('Próchnicza', 0.70, 0.90, 0.55, 0.90),
('Kamienista', 0.10, 0.10, 0.50, 0.10)
;

CREATE TABLE Roslina (
    ID INT AUTO_INCREMENT PRIMARY KEY,
    Nazwa VARCHAR(100) NOT NULL,
    Tekstura2D VARCHAR(255) NOT NULL,
    MaxWysokosc DECIMAL(5, 2) NOT NULL,
    MaxSzerokosc DECIMAL(5, 2) NOT NULL,
    GlebokoscKorzeni DECIMAL(5, 2) NOT NULL,
    PromienKorony DECIMAL(5, 2) NOT NULL,
    PreferencjeSlonca DECIMAL(3, 1) NOT NULL
);

INSERT INTO Roslina (Nazwa, Tekstura2D, MaxWysokosc, MaxSzerokosc, GlebokoscKorzeni, PromienKorony, PreferencjeSlonca) VALUES
('Róża', 'res://textures/rose_placeholder.png', 1.50, 1.00, 0.50, 0.50, 8.0),
('Dąb', 'res://textures/oak_placeholder.png', 25.00, 15.00, 3.00, 7.50, 6.0),
('Paproć', 'res://textures/fern_placeholder.png', 0.70, 0.80, 0.30, 0.40, 2.0);


CREATE TABLE ZasadyWzrostu (
    ID INT AUTO_INCREMENT PRIMARY KEY,
    ID_Roslina INT,
    ID_Gleby INT,
    WspolczynnikWzrostu DECIMAL(3, 2) NOT NULL,
    FOREIGN KEY (ID_Roslina) REFERENCES Roslina(ID),
    FOREIGN KEY (ID_Gleby) REFERENCES TypGleby(ID)
);

INSERT INTO ZasadyWzrostu (ID_Roslina, ID_Gleby, WspolczynnikWzrostu) VALUES
(1, 1, 1.10),
(1, 2, 0.70),
(2, 3, 1.25),
(3, 3, 1.05);
