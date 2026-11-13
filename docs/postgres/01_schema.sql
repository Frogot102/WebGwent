--
-- PostgreSQL database dump
--


-- Dumped from database version 18.2
-- Dumped by pg_dump version 18.2

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: accounts; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.accounts (
    "Id" uuid NOT NULL,
    "Username" character varying(64) NOT NULL,
    "DisplayName" character varying(64) NOT NULL,
    "PasswordHash" text NOT NULL,
    "PasswordSalt" text NOT NULL,
    "Rating" integer NOT NULL,
    "FavoriteFaction" character varying(64) NOT NULL,
    "AvatarId" character varying(32) NOT NULL,
    "AvatarVersion" bigint NOT NULL,
    "MatchesVsPlayers" integer NOT NULL,
    "WinsVsPlayers" integer NOT NULL,
    "LossesVsPlayers" integer NOT NULL,
    "DrawsVsPlayers" integer NOT NULL,
    "MatchesVsBot" integer NOT NULL,
    "WinsVsBot" integer NOT NULL,
    "LossesVsBot" integer NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "LastLoginUtc" timestamp with time zone NOT NULL,
    "Role" character varying(32) NOT NULL,
    "PlusUntilUtc" timestamp with time zone
);


--
-- Name: decks; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.decks (
    "Id" uuid NOT NULL,
    "Name" character varying(128) NOT NULL,
    "Faction" character varying(64) NOT NULL,
    "LeaderId" character varying(128) NOT NULL,
    "CardIds" jsonb NOT NULL,
    "OwnerAccountId" uuid,
    "UpdatedUtc" timestamp with time zone NOT NULL
);


--
-- Name: invite_requests; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.invite_requests (
    "Id" text NOT NULL,
    "AccountId" uuid NOT NULL,
    "Username" character varying(64) NOT NULL,
    "DisplayName" character varying(64) NOT NULL,
    "RequestedDays" integer NOT NULL,
    "Status" character varying(32) NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "ResolvedAtUtc" timestamp with time zone,
    "ResolvedByAccountId" uuid,
    "GrantedInviteCode" text
);


--
-- Name: invites; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.invites (
    "Id" text NOT NULL,
    "Code" character varying(64) NOT NULL,
    "DurationDays" integer NOT NULL,
    "MaxActivations" integer NOT NULL,
    "UsedCount" integer NOT NULL,
    "CreatedByAccountId" uuid NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "ExpiresAtUtc" timestamp with time zone,
    "Revoked" boolean NOT NULL,
    "Redemptions" jsonb NOT NULL
);


--
-- Name: match_history; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.match_history (
    "Id" uuid NOT NULL,
    "PlayedAtUtc" timestamp with time zone NOT NULL,
    "Mode" character varying(32) NOT NULL,
    "PlayerAccountId" uuid,
    "OpponentAccountId" uuid,
    "PlayerName" text NOT NULL,
    "OpponentName" text NOT NULL,
    "PlayerFaction" character varying(64) NOT NULL,
    "OpponentFaction" character varying(64) NOT NULL,
    "Result" character varying(16) NOT NULL,
    "PlayerRoundWins" integer NOT NULL,
    "OpponentRoundWins" integer NOT NULL,
    "RatingBefore" integer NOT NULL,
    "RatingDelta" integer NOT NULL,
    "MatchId" uuid NOT NULL,
    "BotDifficulty" text,
    "RoundScores" jsonb NOT NULL
);


--
-- Name: accounts PK_accounts; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.accounts
    ADD CONSTRAINT "PK_accounts" PRIMARY KEY ("Id");


--
-- Name: decks PK_decks; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.decks
    ADD CONSTRAINT "PK_decks" PRIMARY KEY ("Id");


--
-- Name: invite_requests PK_invite_requests; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.invite_requests
    ADD CONSTRAINT "PK_invite_requests" PRIMARY KEY ("Id");


--
-- Name: invites PK_invites; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.invites
    ADD CONSTRAINT "PK_invites" PRIMARY KEY ("Id");


--
-- Name: match_history PK_match_history; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.match_history
    ADD CONSTRAINT "PK_match_history" PRIMARY KEY ("Id");


--
-- Name: IX_accounts_Username; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_accounts_Username" ON public.accounts USING btree ("Username");


--
-- Name: IX_decks_OwnerAccountId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_decks_OwnerAccountId" ON public.decks USING btree ("OwnerAccountId");


--
-- Name: IX_invites_Code; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_invites_Code" ON public.invites USING btree ("Code");


--
-- Name: IX_match_history_MatchId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_match_history_MatchId" ON public.match_history USING btree ("MatchId");


--
-- Name: IX_match_history_PlayerAccountId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_match_history_PlayerAccountId" ON public.match_history USING btree ("PlayerAccountId");


--
-- Name: decks fk_decks_owner; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.decks
    ADD CONSTRAINT fk_decks_owner FOREIGN KEY ("OwnerAccountId") REFERENCES public.accounts("Id") ON DELETE SET NULL;


--
-- Name: invite_requests fk_invite_requests_account; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.invite_requests
    ADD CONSTRAINT fk_invite_requests_account FOREIGN KEY ("AccountId") REFERENCES public.accounts("Id") ON DELETE CASCADE;


--
-- Name: invite_requests fk_invite_requests_resolved_by; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.invite_requests
    ADD CONSTRAINT fk_invite_requests_resolved_by FOREIGN KEY ("ResolvedByAccountId") REFERENCES public.accounts("Id") ON DELETE SET NULL;


--
-- Name: invites fk_invites_created_by; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.invites
    ADD CONSTRAINT fk_invites_created_by FOREIGN KEY ("CreatedByAccountId") REFERENCES public.accounts("Id") ON DELETE CASCADE;


--
-- Name: match_history fk_match_opponent; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.match_history
    ADD CONSTRAINT fk_match_opponent FOREIGN KEY ("OpponentAccountId") REFERENCES public.accounts("Id") ON DELETE SET NULL;


--
-- Name: match_history fk_match_player; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.match_history
    ADD CONSTRAINT fk_match_player FOREIGN KEY ("PlayerAccountId") REFERENCES public.accounts("Id") ON DELETE SET NULL;


--
-- PostgreSQL database dump complete
--


