﻿// <auto-generated />
using System;
using CurrencyTelegramBot.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CurrencyTelegramBot.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20250609183709_CriarTabelaUsuarios")]
    partial class CriarTabelaUsuarios
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("CurrencyTelegramBot.Entity.User", b =>
                {
                    b.Property<long>("ChatId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("ChatId"));

                    b.Property<bool>("IsActive")
                        .HasColumnType("boolean");

                    b.Property<DateTime>("LastNotify")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("MinutesInterval")
                        .HasColumnType("integer");

                    b.Property<int>("State")
                        .HasColumnType("integer");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("ChatId");

                    b.ToTable("User");
                });

            modelBuilder.Entity("CurrencyTelegramBot.Entity.UserCoins", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<long>("ChatId")
                        .HasColumnType("bigint");

                    b.Property<string>("From")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("To")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("ChatId");

                    b.ToTable("UserCoins");
                });

            modelBuilder.Entity("CurrencyTelegramBot.Entity.UserCoins", b =>
                {
                    b.HasOne("CurrencyTelegramBot.Entity.User", "User")
                        .WithMany("Coins")
                        .HasForeignKey("ChatId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("CurrencyTelegramBot.Entity.User", b =>
                {
                    b.Navigation("Coins");
                });
#pragma warning restore 612, 618
        }
    }
}
